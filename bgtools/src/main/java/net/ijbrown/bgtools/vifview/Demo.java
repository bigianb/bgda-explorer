package net.ijbrown.bgtools.vifview;

import com.google.devtools.common.options.OptionsParser;
import org.lwjgl.*;
import org.lwjgl.glfw.*;
import org.lwjgl.opengl.*;
import org.lwjgl.system.*;

import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.*;
import java.util.Collections;

import static org.lwjgl.glfw.Callbacks.*;
import static org.lwjgl.glfw.GLFW.*;
import static org.lwjgl.opengl.GL11.*;
import static org.lwjgl.opengl.GL11C.GL_RENDERER;
import static org.lwjgl.opengl.GL11C.GL_VENDOR;
import static org.lwjgl.opengl.GL11C.GL_VERSION;
import static org.lwjgl.opengl.GL11C.glGetString;
import static org.lwjgl.system.MemoryStack.*;
import static org.lwjgl.system.MemoryUtil.*;

public class Demo {

    // The window handle
    private final Window window;

    private final Renderer renderer;
    private final Camera camera;

    public Demo()
    {

        renderer = new Renderer();
        camera = new Camera();
        window = new Window("VIF Viewer", 600, 600, true);
    }


    public void run(String gameDir, String characterName) throws Exception {
        GameConfigs cfg = new GameConfigs();
        try {
            cfg.read();
        } catch (IOException | URISyntaxException e) {
            throw new IllegalStateException("Failed to read config");
        }

        GameDataManager gameDataManager = new GameDataManager(cfg, gameDir);
        gameDataManager.discover();

        GameConfig.Character characterConfig = gameDataManager.findCharacter(characterName);
        if (characterConfig == null){
            throw new RuntimeException("Couldn't find character config for " + characterName);
        }
        System.out.println("Using LWJGL " + Version.getVersion() + "!");

        init();
        loop(gameDataManager, characterConfig);

        renderer.cleanup();
    }

    private void init() throws Exception {
        window.init();
        renderer.init(window);
    }

    private void loop(GameDataManager gameDataManager, GameConfig.Character characterConfig) throws IOException {

        CharacterModel characterModel = new CharacterModel(gameDataManager, characterConfig);
        characterModel.read();

        characterModel.setPosition(0, 0, -2);

        CharacterModel[] items = new CharacterModel[1];
        items[0] = characterModel;

        System.err.println("GL_VENDOR: " + glGetString(GL_VENDOR));
        System.err.println("GL_RENDERER: " + glGetString(GL_RENDERER));
        System.err.println("GL_VERSION: " + glGetString(GL_VERSION));

        // Run the rendering loop until the user has attempted to close
        // the window or has pressed the ESCAPE key.
        while ( !window.windowShouldClose() ) {
            renderer.render(window, camera, items);

            window.update();
        }
    }

    public static void main(String[] args) throws Exception {

        OptionsParser parser = OptionsParser.newOptionsParser(CliOptions.class);
        parser.parseAndExitUponError(args);
        CliOptions options = parser.getOptions(CliOptions.class);
        if (options == null || options.dir.isEmpty() || options.help) {
            printUsage(parser);
            return;
        }

        new Demo().run(options.dir, options.character);
    }

    private static void printUsage(OptionsParser parser) {
        System.out.println("Usage: java -jar demo.jar OPTIONS");
        System.out.println(parser.describeOptions(Collections.emptyMap(), OptionsParser.HelpVerbosity.LONG));
    }
}
