package net.ijbrown.bgtools.vifview;

import java.io.IOException;
import java.net.URISyntaxException;

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

        Gltf gltf = new Gltf(characterModel.getMeshes(), characterModel.getTexture());
        gltf.write("", exportFilename);
    }
}
