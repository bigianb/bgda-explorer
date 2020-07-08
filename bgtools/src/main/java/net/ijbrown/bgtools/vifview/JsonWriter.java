package net.ijbrown.bgtools.vifview;

import java.io.BufferedWriter;
import java.io.IOException;

public class JsonWriter {

    private final BufferedWriter writer;
    private boolean needsComma = false;
    private int indent=0;

    public JsonWriter(BufferedWriter writer) {
        this.writer = writer;
    }

    public void writeKeyValue(String key, String value) throws IOException {
        writeKey(key);
        writeValue(value);
    }

    public void writeKeyValue(String key, int value) throws IOException {
        writeKey(key);
        writeValue(value);
    }

    public void writeKeyValue(String key, float[] value) throws IOException {
        writeKey(key);
        writeValue(value);
    }

    public void writeValue(float[] value) throws IOException {
        openArray();
        for (var f : value){
            writeValue(f);
        }
        closeArray();
    }

    public void writeValue(float value) throws IOException {
        if (needsComma){
            writer.append(",");
        }
        writer.append(Float.toString(value));
        needsComma = true;
    }

    public void writeValue(String value) throws IOException {
        if (needsComma){
            writer.append(",");
        }
        writer.append('"');
        writer.append(value);
        writer.append('"');
        needsComma = true;
    }

    public void writeValue(int value) throws IOException {
        if (needsComma){
            writer.append(",");
        }
        writer.append(Integer.toString(value));
        needsComma = true;
    }

    public void writeKey(String asset) throws IOException {
        if (needsComma){
            writer.append(",");
        }
        writeNewline();
        writer.append('"');
        writer.append(asset);
        writer.append("\": ");
        needsComma= false;
    }

    public void append(String s) throws IOException {
        writer.append(s);
    }

    public void append(int id) throws IOException {
        writer.append(Integer.toString(id));
    }

    public void writeNewline() throws IOException {
        writer.append('\n');
        writeIndent();
    }

    public void writeIndent() throws IOException {
        for (int i=0; i<indent; ++i){
            writer.append(' ');
        }
    }

    public void openObject() throws IOException {
        if (needsComma){
            writer.append(",");
            writeNewline();
        }

        writer.append("{");
        indent += 2;
        needsComma = false;
    }
    public void closeObject() throws IOException {
        indent -= 2;
        writeNewline();
        writer.append("}");
        needsComma = true;
    }

    public void openArray() throws IOException {
        if (needsComma){
            writer.append(",");
            writeNewline();
        }

        writer.append("[");
        indent += 2;
        needsComma = false;
    }
    public void closeArray() throws IOException {
        indent -= 2;
        
        writer.append("]");
        needsComma = true;
    }
}
