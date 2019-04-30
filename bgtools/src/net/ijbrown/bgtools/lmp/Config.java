package net.ijbrown.bgtools.lmp;

public class Config {
    public String getRootDir()
    {
        String osName = System.getProperty("os.name").toLowerCase();
        boolean isMacOs = osName.startsWith("mac os x");

        String rootDir = "/emu/bgda//";
        if (isMacOs){
            String home = System.getProperty("user.home");
            rootDir = home+"/DARK_ALLIANCE/";
        }
        return rootDir;
    }
}
