package net.ijbrown.bgtools.vifview;

import com.google.gson.Gson;

import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.net.URISyntaxException;
import java.net.URL;
import java.net.URLClassLoader;

public class GameConfigs {

    public void read() throws IOException, URISyntaxException {

        var loader = getClass().getClassLoader();

        URL url = loader.getResource("net/ijbrown/bgtools/config/gamesConfig.json");
        if (url == null){
            throw new IOException("cannot build URL");
        }
        File configFile = new File(url.toURI());

        Gson gson = new Gson();
        gameConfigs = gson.fromJson(new FileReader(configFile), GameConfig[].class);
    }

    public GameConfig[] gameConfigs = null;

}
