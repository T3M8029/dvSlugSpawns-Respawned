using DV.Logic.Job;
using DV.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dvSlugSpawnsMod
{
    public static class TrackConfig
    {
        public static HashSet<SpawnRecord> Config = [];

        public static SpawnRecord Create(Track? track = null, string? trackID = null, string? title = null, bool tryOccupied = false)
        {
            string? stationID;
            RailTrack? railTrack;
            if (track != null)
            {
                railTrack = track.RailTrack();
                trackID = track.ID.FullDisplayID;
            }
            else if (trackID != null)
            {
                railTrack = SingletonBehaviour<RailTrackRegistryBase>.Instance.OrderedRailtracks.FirstOrDefault(rt => rt?.LogicTrack()?.ID.FullDisplayID == trackID);
                track = railTrack?.LogicTrack();
            }
            else
            {
                return new();
            }

            if (railTrack == null || track == null || trackID == null) return new();

            int railTrackIndex = Array.IndexOf<RailTrack>(SingletonBehaviour<RailTrackRegistryBase>.Instance.OrderedRailtracks, railTrack);
            if (railTrackIndex == -1) return new();
            stationID = track.ID.yardId;
            if (stationID.Contains("#"))
            {
                stationID = StationController.allStations?.OrderBy(sc => (railTrack.transform.position - sc.gameObject.transform.position).sqrMagnitude).FirstOrDefault().stationInfo.YardID;
            }
            if (stationID == null) return new();
            title ??= "Player created slug spawn: " + track.ID.FullDisplayID;

            SpawnRecord record = new() { Title = title, OrderedRailTrackArrIndex = railTrackIndex, StationID = stationID, TrackID = trackID, TryOccupied = tryOccupied };
            Config.Add(record);
            return record;
        }

        public static bool Save()
        {
            try
            {
                string configFilePath = Path.Combine(Main.ModEntry.Path, "Config.json");
                var data = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(configFilePath, data);
                return true;
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.LogException("Failed to save config", ex);
            }

            return false;
        }

        public static bool Load()
        {
            try
            {
                if (!WorldStreamingInit.IsLoaded) return false;

                string configFilePath = Path.Combine(Main.ModEntry.Path, "Config.json");
                string json = File.ReadAllText(configFilePath);
                if (json != null && json != string.Empty)
                {
                    var data = JsonConvert.DeserializeObject<HashSet<SpawnRecord>>(json);
                    if (data != null)
                    {
                        if (data.Count < 2)
                        {
                            LoadDefaults();
                            return false;
                        }
                        Config = data;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.LogException("Failed to load config, defaults will be loaded", ex);
            }
            LoadDefaults();
            return false;
        }

        private static void LoadDefaults()
        {
            Create(trackID: "HB-A2P", title: "Default: HB Parking", tryOccupied: true);
            Create(trackID: "CME-A1P", title: "Default: CME Parking", tryOccupied: true);
            Create(trackID: "CW-A2D", title: "Default: CW Maintanance", tryOccupied: true);
            Create(trackID: "MF-A1P", title: "Default: MF Parking", tryOccupied: true);
            Create(trackID: "GF-A5P", title: "Default: GF Parking", tryOccupied: false);
            Create(trackID: "OR-A1L", title: "Default: OR Service", tryOccupied: false);
            Create(trackID: "#Y-#S161#T", title: "Default: Starting area", tryOccupied: false);
            Create(trackID: "#Y-#S233#T", title: "Default: Starting area", tryOccupied: false);
        }

#nullable disable
        public record SpawnRecord
        {
            [JsonProperty]
            public string Title { get; set; }

            [JsonProperty]
            public string StationID { get; set; }

            [JsonProperty]
            public string TrackID { get; set; }

            [JsonProperty]
            public int OrderedRailTrackArrIndex { get; set; }

            [JsonProperty]
            public bool TryOccupied { get; set; }
        }
#nullable enable
    }
}