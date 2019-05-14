package net.ijbrown.bgtools.lmp;

public class Config {

    private final GameType gameType;

    public Config(GameType gameType)
    {
        this.gameType = gameType;
    }


    public String getRootDir()
    {
        String osName = System.getProperty("os.name").toLowerCase();
        boolean isMacOs = osName.startsWith("mac os x");

        String rootDir = "/emu/bgda/";
        if (isMacOs){
            String home = System.getProperty("user.home");
            rootDir = home + "/ps2_games/";
        }
        switch(gameType)
        {
            case DARK_ALLIANCE:
                rootDir += "/DARK_ALLIANCE/";
                break;
            case CHAMPIONS_OF_NORRATH:
                rootDir += "/CHAMPIONS_OF_NORRATH/";
                break;
            case JUSTICE_LEAGUE_HEROES:
                rootDir += "/JUSTICE_LEAGUE_HEROES/";
                break;
        }

        return rootDir;
    }

    public String getDataDir()
    {
        String rootDir = getRootDir();
        switch(gameType)
        {
            case DARK_ALLIANCE:
                rootDir += "/BG/DATA/";
                break;
            case CHAMPIONS_OF_NORRATH:
                rootDir += "/BG/DATA/";
                break;
            case JUSTICE_LEAGUE_HEROES:
                rootDir += "/GAME/DATA/";
                break;
        }
        return rootDir;
    }
}
