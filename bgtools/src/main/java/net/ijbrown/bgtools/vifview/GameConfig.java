package net.ijbrown.bgtools.vifview;

import net.ijbrown.bgtools.lmp.GameType;

public class GameConfig {
    public String title;
    public String[] ids;
    public String dataDir;
    public Character[] characters;
    public GameType type;

    public static class Character
    {
        public String name;
        public String lmp;
        public VifDef body;
        public VifDef[] extras;
    }

    public static class VifDef
    {
        public String vif;
        public String tex;
    }
}
