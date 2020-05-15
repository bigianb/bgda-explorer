package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.Lmp;
import net.ijbrown.bgtools.lmp.VifDecode;
import org.joml.Vector3f;
import org.lwjgl.system.MemoryUtil;

import static org.lwjgl.opengl.GL30C.*;
import static org.lwjgl.system.MemoryUtil.memFree;

import java.io.IOException;
import java.nio.FloatBuffer;
import java.util.List;


public class CharacterModel implements IGameItem
{
    private Lmp lmp;
    private GameConfig.Character characterConfig;
    private GameDataManager gameDataManager;
    private List<VifDecode.Mesh> bodyMeshes;

    public CharacterModel(GameDataManager gameDataManager, GameConfig.Character characterConfig) {
        this.gameDataManager = gameDataManager;
        this.characterConfig = characterConfig;

        position = new Vector3f();
        scale = 0.1f;
        rotation = new Vector3f();
    }

    public void read() throws IOException {
        lmp = gameDataManager.getLmp(characterConfig.lmp);
        var bodyVif = lmp.findEntry(characterConfig.body.vif);
        bodyVif = lmp.findEntry(characterConfig.extras[2].vif);    // tophat for testing
        bodyMeshes = new VifDecode().decode(bodyVif.data, bodyVif.offset);
    }

    @Override
    public void render(ShaderProgram shaderProgram) {

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
            glVertexAttribPointer(0, 3, GL_FLOAT, false, 0, 0);

            glEnableVertexAttribArray(0);

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
            out[i++] = v.x;
            out[i++] = v.y;
            out[i++] = v.z;
        }
        return out;
    }

    private final Vector3f position;

    private float scale;

    private final Vector3f rotation;

    @Override
    public Vector3f getRotation() {
        return rotation;
    }

    @Override
    public Vector3f getPosition() {
        return position;
    }

    @Override
    public float getScale() {
        return scale;
    }

    @Override
    public void setPosition(float x, float y, float z) {
        position.x = x;
        position.y = y;
        position.z = z;
    }


}
