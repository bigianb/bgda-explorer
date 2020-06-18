package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.VifDecode;

import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Base64;
import java.util.List;

// https://sandbox.babylonjs.com/ can view files

public class Gltf
{
    private List<VifDecode.Mesh> meshes;
    private Texture texture;

    private static class Node
    {
        public int id;
        public String name;
        public int mesh = -1;
        public int camera = -1;
        public List<Node> children;

        public Node(String name) {
            id = -1;
            this.name = name;
        }
    }

    private List<Node> nodes = new ArrayList<>();

    private static class Buffer
    {
        public int id;
        public byte[] buffer;
    }

    public Gltf(List<VifDecode.Mesh> meshes, Texture texture) {
        this.meshes = meshes;
        this.texture = texture;
    }

    public void write(String dir, String name) throws IOException {
        String gltfName = name + ".gltf";
        var path = Paths.get(dir, gltfName);

        Charset charset = StandardCharsets.UTF_8;
        try (var writer = Files.newBufferedWriter(path, charset)) {
            JsonWriter jsonWriter = new JsonWriter(writer);
            write(jsonWriter);
        }
    }

    public void write(JsonWriter writer) throws IOException {
        writer.openObject();
        writeAsset(writer);
        Node sceneRootNode = new Node("scene");
        nodes.add(sceneRootNode);
        sceneRootNode.id = nodes.size()-1;
        writeScene(writer, sceneRootNode);

        writer.writeKey("meshes");
        writer.openArray();

        // Just the first mesh for now
        var mesh = meshes.get(0);
        Node meshNode = new Node("");
        nodes.add(meshNode);
        meshNode.id = nodes.size()-1;
        meshNode.mesh = 0;

        writeMesh(writer, mesh);

        writer.closeArray();

        writeNodes(writer, "nodes", nodes);
        writeBuffers(writer);
        writeAccessors(writer);
        writer.closeObject();
    }

    private List<Buffer> buffers = new ArrayList<>();

    private Buffer createBuffer(int size)
    {
        Buffer b = new Buffer();
        b.id = buffers.size();
        b.buffer = new byte[size];
        buffers.add(b);
        return b;
    }


    private void writeBuffers(JsonWriter writer) throws IOException {
        // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#reference-buffer
        writer.writeKey("buffers");
        writer.openArray();
        for (var buffer : buffers){
            writer.openObject();
            writer.writeKeyValue("byteLength", buffer.buffer.length);
            // Embedded buffers
            writer.writeKey("uri");
            writeEmbeddedData(writer, buffer.buffer);
            writer.closeObject();
        }
        writer.closeArray();

        // For this implementation, each buffer has a single bufferview of the same index.
        writer.writeKey("bufferViews");
        writer.openArray();
        for (var buffer : buffers){
            writer.openObject();
            writer.writeKeyValue("buffer", buffer.id);
            writer.writeKeyValue("byteOffset", 0);
            writer.writeKeyValue("byteLength", buffer.buffer.length);
            writer.closeObject();
        }
        writer.closeArray();

    }

    private void writeEmbeddedData(JsonWriter writer, byte[] buffer) throws IOException {
        var encoder = Base64.getEncoder();
        String encodedData = encoder.encodeToString(buffer);
        writer.writeValue("data:application/octet-stream;base64,"+encodedData);
    }

    private void writeMesh(JsonWriter writer, VifDecode.Mesh mesh) throws IOException {
        /*
            In glTF, meshes are defined as arrays of primitives.
            Primitives correspond to the data required for GPU draw calls.
            Primitives specify one or more attributes, corresponding to the vertex attributes used in the draw calls.
            Indexed primitives also define an indices property.
            Attributes and indices are defined as references to accessors containing corresponding data.
            Each primitive also specifies a material and a primitive type that corresponds to the GPU primitive type
            (e.g., triangle set).
         */
        var accessors = buildAccessors(mesh);
        writer.openObject();
        writer.writeKey("primitives");
        writer.openArray();
        writer.openObject();
        writer.writeKeyValue("mode", 4);    // triangles
        writer.writeKey("attributes");
        writer.openObject();
        writer.writeKeyValue("POSITION", accessors.positionAccessor.id);
        writer.closeObject();
        writer.writeKeyValue("indices", accessors.indicesAccessor.id);
        writer.closeObject();
        writer.closeArray();
        writer.closeObject();
    }

    private enum ComponentType
    {
        UNSIGNED_SHORT(5123), FLOAT(5126);

        private final int id;

        ComponentType(int id)
        {
            this.id = id;
        }
    }

    private static class Accessor
    {
        public int id;
        public int bufferView;
        public int byteOffset;
        public int count;
        public String type;

        public ComponentType componentType;
    }

    private void writeAccessors(JsonWriter writer) throws IOException {
        // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#accessors

        writer.writeKey("accessors");
        writer.openArray();
        for (var accessor : accessors){
            writer.openObject();
            writer.writeKeyValue("bufferView", accessor.bufferView);
            writer.writeKeyValue("byteOffset", accessor.byteOffset);
            writer.writeKeyValue("count", accessor.count);
            writer.writeKeyValue("type", accessor.type);
            writer.writeKeyValue("componentType", accessor.componentType.id);
            writer.closeObject();
        }
        writer.closeArray();
    }

    private Accessor createAccessor(int bufferView, int byteOffset, int count, String type, ComponentType componentType)
    {
        Accessor accessor = new Accessor();
        accessor.id = accessors.size();
        accessor.bufferView = bufferView;
        accessor.byteOffset = byteOffset;
        accessor.count = count;
        accessor.type = type;
        accessor.componentType = componentType;
        accessors.add(accessor);
        return accessor;
    }

    private List<Accessor> accessors = new ArrayList<>();

    private static class MeshPrimAccessors
    {
        public Accessor positionAccessor;
        public Accessor indicesAccessor;
    }

    private MeshPrimAccessors buildAccessors(VifDecode.Mesh mesh) {
        int positionSize = mesh.vertices.size() * 12;
        int indicesSize = mesh.triangleIndices.size() * 2;
        int bufferSize =  positionSize + indicesSize;
        var buffer = createBuffer(bufferSize);
        int i=0;
        for (var vec3 : mesh.vertices){
            i = writeFloat(buffer.buffer, i, vec3.x);
            i = writeFloat(buffer.buffer, i, vec3.y);
            i = writeFloat(buffer.buffer, i, vec3.z);
        }
        for (var val : mesh.triangleIndices){
            i = writeShort(buffer.buffer, i, val);
        }

        var meshPrimAccessors = new MeshPrimAccessors();
        meshPrimAccessors.positionAccessor = createAccessor(buffer.id, 0, mesh.vertices.size(), "VEC3", ComponentType.FLOAT);
        meshPrimAccessors.indicesAccessor = createAccessor(buffer.id, positionSize, mesh.triangleIndices.size(), "SCALAR", ComponentType.UNSIGNED_SHORT);
        return meshPrimAccessors;
    }

    private int writeShort(byte[] buf, int idx, Integer val) {
        buf[idx++] = (byte)(val & 0xFF);
        buf[idx++] = (byte)((val >> 8) & 0xFF);
        return idx;
    }

    private int writeFloat(byte[] buf, int idx, float f)
    {
        int bits = Float.floatToRawIntBits(f);
        buf[idx++] = (byte)(bits & 0xFF);
        buf[idx++] = (byte)((bits >> 8) & 0xFF);
        buf[idx++] = (byte)((bits >> 16) & 0xFF);
        buf[idx++] = (byte)((bits >> 24) & 0xFF);
        return idx;
    }

    private void writeNodes(JsonWriter writer, String name, List<Node> nodes) throws IOException {
        writer.writeKey(name);
        writer.openArray();
        for (Node node : nodes) {
            writeNode(writer, node);
        }
        writer.closeArray();
    }

    private void writeNode(JsonWriter writer, Node node) throws IOException {
        writer.openObject();
        if (node.name != null && !node.name.isEmpty()){
            writer.writeKeyValue("name", node.name);
        }
        if (node.mesh >= 0){
            writer.writeKeyValue("name", node.name);
        }
        if (node.children != null && !node.children.isEmpty()){
            writeNodes(writer, "children", node.children);
        }
        writer.closeObject();
    }

    // write scene 0 which has a single node with id 0
    private void writeScene(JsonWriter writer, Node rootNode) throws IOException {
        writer.writeKeyValue("scene", 0);
        writer.writeKey("scenes");
        writer.openArray();
        writer.openObject();
        writer.writeKey("nodes");
        writer.openArray();
        writer.append(rootNode.id);
        writer.closeArray();
        writer.closeObject();
        writer.closeArray();
    }

    private void writeAsset(JsonWriter writer) throws IOException {
        writer.writeKey("asset");
        writer.openObject();
        writer.writeKeyValue("version", "2.0");
        writer.writeKeyValue("generator", "bgdatools");
        writer.closeObject();
    }



}
