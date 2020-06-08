package net.ijbrown.bgtools.vifview;

import java.io.BufferedWriter;
import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;

public class Gltf
{

    public void write(String dir, String name) throws IOException {
        String gltfName = name + ".gltf";
        var path = Paths.get(dir, gltfName);

        Charset charset = StandardCharsets.UTF_8;
        try (var writer = Files.newBufferedWriter(path, charset)) {
            write(writer);
        }
    }

    public void write(BufferedWriter writer)
    {

    }

}
