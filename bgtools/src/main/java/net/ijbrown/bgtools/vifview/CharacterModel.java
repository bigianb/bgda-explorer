package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.Lmp;
import net.ijbrown.bgtools.lmp.VifDecode;
import org.joml.Matrix4d;
import org.joml.Matrix4x3d;
import org.lwjgl.system.MemoryUtil;

import static org.lwjgl.opengl.GL30C.*;
import static org.lwjgl.system.MemoryUtil.memFree;

import java.io.IOException;
import java.nio.FloatBuffer;
import java.util.List;


public class CharacterModel
{
    private Lmp lmp;
    private GameConfig.Character characterConfig;
    private GameDataManager gameDataManager;
    private List<VifDecode.Mesh> bodyMeshes;

    public CharacterModel(GameDataManager gameDataManager, GameConfig.Character characterConfig) {
        this.gameDataManager = gameDataManager;
        this.characterConfig = characterConfig;
    }

    public void read() throws IOException {
        lmp = gameDataManager.getLmp(characterConfig.lmp);
        var bodyVif = lmp.findEntry(characterConfig.body.vif);
        bodyVif = lmp.findEntry(characterConfig.extras[2].vif);    // tophat for testing
        bodyMeshes = new VifDecode().decode(bodyVif.data, bodyVif.offset);
    }

    static String vertexShaderText =
        "#version 330\n"+
        "uniform mat4 u_MVP;\n"+
        "uniform mat3 u_NORMAL;\n"+
        "uniform vec3 u_LIGHT;\n"+

        "in vec3 in_Position;\n"+
        "in vec3 in_Normal;\n"+
        "out float v_Shade;\n"+

        "void main()\n"+
        "{\n"+
        //"    vec3 normal = normalize(u_NORMAL * in_Normal);\n"+
        //"    v_Shade = max(dot(normal, u_LIGHT), 0.0);\n"+
        //"    gl_Position = u_MVP * vec4(in_Position, 1.0);\n"+
                "    v_Shade=1.0;\n"+
                "    gl_Position = vec4(in_Position, 1.0);\n"+
        "}\n";

    static String fragmentShaderText =
            "#version 330\n"+
            "uniform vec4 u_COLOR;\n" +
            "in float v_Shade;\n" +
            "out vec4 out_Color;\n" +

            "void main() {\n" +
            //"    out_Color = vec4(u_COLOR.xyz * v_Shade, u_COLOR.w);\n" +
                    "    out_Color = vec4(0.0, 0.5, 0.5, v_Shade);\n" +

            "}";

    private static int compileShaders(String vs, String fs) {
        int v = glCreateShader(GL_VERTEX_SHADER);
        int f = glCreateShader(GL_FRAGMENT_SHADER);

        compileShader(v, vs);
        compileShader(f, fs);

        int p = glCreateProgram();
        glAttachShader(p, v);
        glAttachShader(p, f);
        glLinkProgram(p);
        printProgramInfoLog(p);

        if (glGetProgrami(p, GL_LINK_STATUS) != GL_TRUE) {
            throw new IllegalStateException("Failed to link program.");
        }

        glUseProgram(p);
        return p;
    }

    private static void printShaderInfoLog(int obj) {
        int infologLength = glGetShaderi(obj, GL_INFO_LOG_LENGTH);
        if (infologLength > 0) {
            glGetShaderInfoLog(obj);
            System.out.format("%s\n", glGetShaderInfoLog(obj));
        }
    }

    private static void printProgramInfoLog(int obj) {
        int infologLength = glGetProgrami(obj, GL_INFO_LOG_LENGTH);
        if (infologLength > 0) {
            glGetProgramInfoLog(obj);
            System.out.format("%s\n", glGetProgramInfoLog(obj));
        }
    }

    private static void compileShader(int shader, String source) {
        glShaderSource(shader, source);

        glCompileShader(shader);
        printShaderInfoLog(shader);

        if (glGetShaderi(shader, GL_COMPILE_STATUS) != GL_TRUE) {
            throw new IllegalStateException("Failed to compile shader.");
        }
    }

    private final Matrix4d
            P   = new Matrix4d(),
            MVP = new Matrix4d();
    private final Matrix4x3d
            V   = new Matrix4x3d(),
            M   = new Matrix4x3d(),
            MV  = new Matrix4x3d();

    public void render() {
        var program = compileShaders(vertexShaderText, fragmentShaderText);
        int u_MVP = glGetUniformLocation(program, "u_MVP");
        int u_NORMAL = glGetUniformLocation(program, "u_NORMAL");
        int u_LIGHT = glGetUniformLocation(program, "u_LIGHT");
        int u_COLOR = glGetUniformLocation(program, "u_COLOR");

        int positions = glGetAttribLocation(program, "in_Position");
        int normals = glGetAttribLocation(program, "in_Normal");

        for (var mesh : bodyMeshes) {

            float[] verticesRaw = upwrapVertices(mesh.vertices);
            FloatBuffer verticesBuffer = MemoryUtil.memAllocFloat(verticesRaw.length);
            verticesBuffer.put(verticesRaw).flip();

            // The attribute array. Holds the buffer array(s)
            var vaoId = glGenVertexArrays();
            glBindVertexArray(vaoId);

            // Copies our data into the vbo on the gpu
            var vboId = glGenBuffers();
            glBindBuffer(GL_ARRAY_BUFFER, vboId);
            glBufferData(GL_ARRAY_BUFFER, verticesBuffer, GL_STATIC_DRAW);
            memFree(verticesBuffer);

            var idxVboId = glGenBuffers();
            int[] indicesRaw = new int[mesh.triangleIndices.size()];
            for (int i=0; i<mesh.triangleIndices.size(); ++i){
                indicesRaw[i] = mesh.triangleIndices.get(i);
            }
            var indicesBuffer = MemoryUtil.memAllocInt(indicesRaw.length);
            indicesBuffer.put(indicesRaw).flip();
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, idxVboId);
            glBufferData(GL_ELEMENT_ARRAY_BUFFER, indicesBuffer, GL_STATIC_DRAW);
            memFree(indicesBuffer);

            // Define the format of the data
            glVertexAttribPointer(positions, 3, GL_FLOAT, false, 0, 0);

            glEnableVertexAttribArray(positions);

            glPolygonMode( GL_FRONT_AND_BACK, GL_LINE );
            glDisable(GL_CULL_FACE);
            glEnable(GL_DEPTH_TEST);
            glDrawElements(GL_TRIANGLES, mesh.triangleIndices.size(), GL_UNSIGNED_INT, 0);
        }
    }

    private float[] upwrapVertices(List<VifDecode.Vec3F> vertices) {
        float[] out = new float[vertices.size()*3];
        int i=0;
        for (var v : vertices){
            // transform is just a hack to show tophat
            out[i++] = v.x / 10.0f + 0.5f;
            out[i++] = v.y / 10.0f;
            out[i++] = v.z / 10.0f + 0.5f;
        }
        return out;
    }
}
