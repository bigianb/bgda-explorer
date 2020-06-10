package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.VifDecode;

import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
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

    private void writeBuffers(JsonWriter writer)
    {
        
    }

    private void writeMesh(JsonWriter writer, VifDecode.Mesh mesh) throws IOException {
        writer.openObject();
        writer.writeKey("primitives");
        writer.openArray();
        writer.openObject();
        writer.writeKeyValue("mode", 4);    // triangles
        writer.closeObject();
        writer.closeArray();
        writer.closeObject();
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
