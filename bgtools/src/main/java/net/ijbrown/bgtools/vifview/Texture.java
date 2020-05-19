package net.ijbrown.bgtools.vifview;

import java.nio.ByteBuffer;
import java.nio.IntBuffer;

import net.ijbrown.bgtools.lmp.TexDecode;
import org.lwjgl.BufferUtils;
import org.lwjgl.system.MemoryStack;

import static org.lwjgl.opengl.GL11.*;
import static org.lwjgl.opengl.GL30.glGenerateMipmap;
import static org.lwjgl.stb.STBImage.*;

public class Texture
{
    private int id;

    public int width, height;


    public void bind() {
        glBindTexture(GL_TEXTURE_2D, id);
    }

    public int getId() {
        return id;
    }

    private static int makePow2(int value)
    {
        int highestOneBit = Integer.highestOneBit(value);
        if (value == highestOneBit) {
            return value;
        }
        return highestOneBit << 1;
    }

    public void loadTexture(TexDecode.DecodedTex tex)
    {
        width = makePow2(tex.pixelsWidth);
        height = makePow2(tex.pixelsHeight);
        ByteBuffer buf = BufferUtils.createByteBuffer(width*height*4);

        int srcIdx=0;
        for (int y=0; y<tex.pixelsHeight; ++y){
            int idx = y * width * 4;
            for (int x=0; x<tex.pixelsWidth; ++x){
                var pixel = tex.pixels[srcIdx++];
                buf.put(idx++, pixel.r);
                buf.put(idx++, pixel.g);
                buf.put(idx++, pixel.b);
                buf.put(idx++, pixel.a);
            }
        }

        // Create a new OpenGL texture
        id = glGenTextures();
        // Bind the texture
        glBindTexture(GL_TEXTURE_2D, id);

        // Tell OpenGL how to unpack the RGBA bytes. Each component is 1 byte size
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        // Upload the texture data
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, buf);
        // Generate Mip Map
        glGenerateMipmap(GL_TEXTURE_2D);

    }

    public void loadTexture(String fileName) throws Exception {

        ByteBuffer buf;
        // Load Texture file
        try (MemoryStack stack = MemoryStack.stackPush()) {
            IntBuffer w = stack.mallocInt(1);
            IntBuffer h = stack.mallocInt(1);
            IntBuffer channels = stack.mallocInt(1);

            buf = stbi_load(fileName, w, h, channels, 4);
            if (buf == null) {
                throw new Exception("Image file [" + fileName  + "] not loaded: " + stbi_failure_reason());
            }

            width = w.get();
            height = h.get();
        }

        // Create a new OpenGL texture
        id = glGenTextures();
        // Bind the texture
        glBindTexture(GL_TEXTURE_2D, id);

        // Tell OpenGL how to unpack the RGBA bytes. Each component is 1 byte size
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

        //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);

        // Upload the texture data
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, buf);
        // Generate Mip Map
        glGenerateMipmap(GL_TEXTURE_2D);

        stbi_image_free(buf);
    }

    public void cleanup() {
        glDeleteTextures(id);
    }
}
