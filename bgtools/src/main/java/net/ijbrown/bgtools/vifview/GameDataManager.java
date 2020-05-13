package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.Lmp;

import java.io.IOException;
import java.nio.file.FileSystems;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;

/**
 * Deals with finding the data for a game.
 */
public class GameDataManager
{
    private final GameConfigs configs;
    private final String rootDir;

    private GameConfig gameConfig = null;

    public GameDataManager(GameConfigs configs, String rootDir){
        this.configs = configs;
        this.rootDir = rootDir;
    }

    /**
     * Looks in the root directory to find out which game this is.
     */
    public GameConfig discover() throws IOException {
        this.gameConfig = null;
        Path rootPath = FileSystems.getDefault().getPath(this.rootDir);
        Object[] contents = Files.list(rootPath).toArray();
        for(Object object : contents){
            Path file = (Path)object;
            Path name = file.getFileName();
            for (GameConfig config : this.configs.gameConfigs){
                for (String id : config.ids){
                    if (name.toString().equals(id)){
                        this.gameConfig = config;
                        break;
                    }
                }
                if (this.gameConfig != null){
                    break;
                }
            }
            if (this.gameConfig != null){
                break;
            }
        }
        return this.gameConfig;
    }

    public GameConfig.Character findCharacter(String characterName)
    {
        GameConfig.Character charObj = null;
        if (this.gameConfig != null){
            for (GameConfig.Character character : this.gameConfig.characters){
                if (characterName.equals(character.name)){
                    charObj = character;
                    break;
                }
            }
        }
        return charObj;
    }

    private final Map<String, Lmp> lmpCache = new HashMap<>();

    public Lmp getLmp(String lmpName) throws IOException {
        if (!lmpCache.containsKey(lmpName)){
            Path rootPath = FileSystems.getDefault().getPath(rootDir);
            Path dataPath = rootPath.resolve(gameConfig.dataDir);
            Path lmpPath = dataPath.resolve(lmpName);
            Lmp lmp = new Lmp(gameConfig.type);
            lmp.readLmpFile(lmpPath);
            lmpCache.put(lmpName, lmp);
        }
        return lmpCache.get(lmpName);
    }
}
