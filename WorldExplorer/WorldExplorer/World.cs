/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using JetBlackEngineLib;
using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Textures;
using JetBlackEngineLib.Data.World;
using System;
using System.IO;

namespace WorldExplorer;

public class World
{
    public readonly string DataPath;
    public readonly EngineVersion EngineVersion;
    public readonly string Name;
    public CacheFile? HdrDatFile;

    // The parsed data from the various files.
    public WorldData? WorldData = null;

    public GobFile? WorldGob;
    public LmpFile? WorldLmp;
    public WorldTexFile? WorldTex;
    public YakFile? WorldYak;

    public World(EngineVersion engineVersion, string dataPath, string name)
    {
        EngineVersion = engineVersion;
        DataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));
        Name = name;
    }

    public void Load()
    {
        var ext = (Path.GetExtension(Name) ?? "").ToLower();

        switch (ext)
        {
            case ".gob":
                var texFileName = Path.GetFileNameWithoutExtension(Name) + ".tex";
                var textFilePath = Path.Combine(DataPath, texFileName);
                WorldGob = new GobFile(EngineVersion, Path.Combine(DataPath, Name));
                if (File.Exists(textFilePath))
                {
                    WorldTex = new WorldTexFile(EngineVersion, textFilePath);
                }
                else
                {
                    WorldTex = null;
                }

                break;
            case ".lmp":
                // TODO: Support just passing the filepath instead of having to load data here
                var data = File.ReadAllBytes(Path.Combine(DataPath, Name));
                WorldLmp = new LmpFile(EngineVersion, Name, data, 0, data.Length);
                break;
            case ".yak":
                var yakData = File.ReadAllBytes(Path.Combine(DataPath, Name));
                WorldYak = new YakFile(EngineVersion, Name, yakData);
                break;
            case ".hdr":
                var baseName = Name[..^4];
                var hdrData = File.ReadAllBytes(Path.Combine(DataPath, Name));
                var datData = File.ReadAllBytes(Path.Combine(DataPath, baseName + ".DAT"));
                HdrDatFile = new CacheFile(EngineVersion, baseName, hdrData, datData);
                break;
            default:
                throw new NotSupportedException("Unsupported file type");
        }
    }
}