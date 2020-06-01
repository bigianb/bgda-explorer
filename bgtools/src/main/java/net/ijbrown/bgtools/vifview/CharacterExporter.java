package net.ijbrown.bgtools.vifview;

import com.google.gson.Gson;

import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Exports a character to glTF format.
 */
public class CharacterExporter
{
    public void export(String gameDir, String characterName, String exportFilename) throws IOException {
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

        CharacterModel characterModel = new CharacterModel(gameDataManager, characterConfig);
        characterModel.read();

        Gson gson = new Gson();
        String json = gson.toJson(new GlTF());
        writeFile(exportFilename, json);
    }

    private void writeFile(String filename, String contents) throws IOException {

        Path path = Paths.get(filename);
        Files.write(path, contents.getBytes("UTF-8"));
    }

    static class GlTF {
        private final Asset asset = new Asset();
        GlTF() {}
    }

    static class Asset {
        private final double version = 2.0;
        private final String generator = "bgdatools";
        Asset(){}
    }
}
