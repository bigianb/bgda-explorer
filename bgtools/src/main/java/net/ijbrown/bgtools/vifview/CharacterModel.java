package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.Lmp;
import net.ijbrown.bgtools.lmp.VifDecode;
import org.joml.Matrix4d;
import org.joml.Matrix4x3d;

import static org.lwjgl.opengl.GL30C.*;

import java.io.IOException;
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
        "    vec3 normal = normalize(u_NORMAL * in_Normal);\n"+
        "    v_Shade = max(dot(normal, u_LIGHT), 0.0);\n"+
        "    gl_Position = u_MVP * vec4(in_Position, 1.0);\n"+
        "}\n";

    static String fragmentShaderText =
            "#version 330\n"+
            "uniform vec4 u_COLOR;\n" +
            "in float v_Shade;\n" +
            "out vec4 out_Color;\n" +

            "void main() {\n" +
            "    out_Color = vec4(u_COLOR.xyz * v_Shade, u_COLOR.w);\n" +
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

        glEnableVertexAttribArray(positions);
        glEnableVertexAttribArray(normals);


    }
}
