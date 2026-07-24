/*using System;
using System.Collections.Generic;
using System.IO;

namespace dvSlugSpawnsMod
{
    internal class CsvConfig
    {
        private static string[] defaultConfigLines = {
            "#yardID,trackID,tryOccupied,comment",
            "#HB,HB-A2P,0,Harbor parking",
            "#CM,CM-A2D,0,Maintenance track",
            "#GF,GF-A4P,1,Parking track",
            "SM,#Y-#S436#T,0,Tutorial shed",
        };

        public static Dictionary<string, List<string>> slugSpawns = new();
        public static Dictionary<string, bool> tryOccupiedTracks = new();

        public static void ReadSpawns()
        {
            string configFilePath = Path.Combine(Main.ModEntry.Path, "slug_spawns.csv");
            if (!File.Exists(configFilePath))
            {
                Main.ModEntry.Logger.Log($"config file not found, creating: {configFilePath}");
                try
                {
                    using (StreamWriter outputFile = new StreamWriter(configFilePath))
                    {
                        foreach (string line in defaultConfigLines)
                        {
                            outputFile.WriteLine(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Main.ModEntry.Logger.LogException("Failed to write config file", ex);
                    return;
                }
            }
            slugSpawns.Clear();
            tryOccupiedTracks.Clear();
            using (StreamReader reader = new StreamReader(configFilePath))
            {
                try
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line[0] == '#' || line.Trim().Length < 1)
                        {
                            continue;
                        }
                        string[] cols = line.Split(',');
                        if (cols.Length < 3 || cols[0].Length < 2 || cols[1].Length < 6 || cols[2].Length < 1)
                        {
                            Main.ModEntry.Logger.Log($"ignoring config line: \"{line}\"");
                            continue;
                        }
                        string stationId = cols[0].Trim();
                        string trackName = cols[1].Trim();
                        bool tryOccupied = cols[2].Trim() == "1";
                        List<string> trackList = new();
                        if (slugSpawns.ContainsKey(stationId))
                        {
                            slugSpawns.TryGetValue(stationId, out trackList);
                        }
                        else
                        {
                            slugSpawns.Add(stationId, trackList);
                        }
                        if (!trackList.Contains(trackName))
                        {
                            trackList.Add(trackName);
                            if (tryOccupied)
                            {
                                tryOccupiedTracks.Add(trackName, tryOccupied);
                            }
                            Main.ModEntry.Logger.Log($"Added potential spawn: stationId:{stationId} trackName:{trackName} tryOccupied:{tryOccupied}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Main.ModEntry.Logger.LogException("Failed to read config file", ex);
                }
            }
        }
    }
}
*/