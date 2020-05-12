package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.Lmp;
import net.ijbrown.bgtools.lmp.VifDecode;

import java.io.IOException;

public class CharacterModel
{
    private Lmp lmp;
    private GameConfig.Character characterConfig;
    private GameDataManager gameDataManager;

    public CharacterModel(GameDataManager gameDataManager, GameConfig.Character characterConfig) {
        this.gameDataManager = gameDataManager;
        this.characterConfig = characterConfig;
    }

    public void read() throws IOException {
        lmp = gameDataManager.getLmp(characterConfig.lmp);
        var bodyVif = lmp.findEntry(characterConfig.body.vif);
         bodyVif = lmp.findEntry(characterConfig.extras[2].vif);    // tophat for testing
        var bodyMeshes = new VifDecode().decode(bodyVif.data, bodyVif.offset);
    }

    public void render() {

    }
}
