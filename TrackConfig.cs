using DV.Logic.Job;
using DV.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UnityEngine;

namespace dvSlugSpawnsMod
{
    public static class TrackConfig
    {
        public static List<SpawnRecord> Config = [];

        private static string newTrackID = "";
        private static string newTitle = "";
        private static bool newTryOccupied = false;

        public static void DrawGUI()
        {
            GUILayout.Label("Custom Slug Spawns", new GUIStyle(GUI.skin.label) { fontStyle = UnityEngine.FontStyle.Bold });
            GUILayout.BeginHorizontal();
            GUILayout.Label("Track ID", GUILayout.Width(150));
            GUILayout.Label("Title", GUILayout.Width(250));
            GUILayout.Label("Try Occupied", GUILayout.Width(100));
            GUILayout.Label("Actions", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            SpawnRecord? recordToRemove = null;

            foreach (var record in Config)
            {
                GUILayout.BeginHorizontal();

                string updatedTrackID = GUILayout.TextField(record.TrackID, GUILayout.Width(150));
                if (updatedTrackID != record.TrackID)
                {
                    record.TrackID = updatedTrackID;
                    record.OrderedRailTrackArrIndex = -1; // Mark as unresolved so it re-evaluates
                    record.StationID = null;
                }

                record.Title = GUILayout.TextField(record.Title, GUILayout.Width(250));
                record.TryOccupied = GUILayout.Toggle(record.TryOccupied, "", GUILayout.Width(100));

                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    recordToRemove = record;
                }

                GUILayout.EndHorizontal();
            }

            if (recordToRemove != null) Config.Remove(recordToRemove);

            GUILayout.BeginHorizontal();
            newTrackID = GUILayout.TextField(newTrackID, GUILayout.Width(150));
            newTitle = GUILayout.TextField(newTitle, GUILayout.Width(250));
            newTryOccupied = GUILayout.Toggle(newTryOccupied, "", GUILayout.Width(100));

            if (GUILayout.Button("Add", GUILayout.Width(80)))
            {
                if (!string.IsNullOrWhiteSpace(newTrackID))
                {
                    Config.Add(new SpawnRecord
                    {
                        TrackID = newTrackID,
                        Title = string.IsNullOrWhiteSpace(newTitle) ? $"Player custom: {newTrackID}" : newTitle,
                        TryOccupied = newTryOccupied,
                        OrderedRailTrackArrIndex = -1
                    });
                    ResolveAllRecords();

                    newTrackID = "";
                    newTitle = "";
                    newTryOccupied = false;
                    Save();
                }
            }

            if (GUILayout.Button("CurrTrack", GUILayout.Width(80)))
            {
                if (WorldStreamingInit.IsLoaded && PlayerManager.Car != null)
                {
                    var foo = PlayerManager.Car?.FrontBogie?.track?.LogicTrack().ID.FullDisplayID;
                    if (foo != null || foo != string.Empty) newTrackID = foo!;
                }
            }

            GUILayout.EndHorizontal();
        }

        public static SpawnRecord Create(Track? track = null, string? trackID = null, string? title = null, bool tryOccupied = false)
        {
            if (track != null) trackID = track.ID.FullDisplayID;
            if (string.IsNullOrEmpty(trackID)) return new SpawnRecord();
            title ??= "Player created slug spawn: " + trackID;
            var record = new SpawnRecord { Title = title, TrackID = trackID, TryOccupied = tryOccupied, OrderedRailTrackArrIndex = -1 };
            Config.Add(record);
            if (WorldStreamingInit.IsLoaded) ResolveRecord(record);
            return record;
        }

        public static void ResolveAllRecords()
        {
            if (!WorldStreamingInit.IsLoaded) return;
            foreach (var record in Config) ResolveRecord(record);
        }

        private static void ResolveRecord(SpawnRecord record)
        {
            if (string.IsNullOrEmpty(record.TrackID)) return;

            var railTrack = SingletonBehaviour<RailTrackRegistryBase>.Instance?.OrderedRailtracks.FirstOrDefault(rt => rt?.LogicTrack()?.ID.FullDisplayID == record.TrackID);
            if (railTrack == null) return;

            var track = railTrack.LogicTrack();
            if (track == null) return;
            record.OrderedRailTrackArrIndex = Array.IndexOf(SingletonBehaviour<RailTrackRegistryBase>.Instance!.OrderedRailtracks, railTrack);

            string stationID = track.ID.yardId;
            if (stationID.Contains("#"))
            {
                var closestStation = StationController.allStations?.OrderBy(sc => (railTrack.transform.position - sc.gameObject.transform.position).sqrMagnitude).FirstOrDefault();
                if (closestStation != null) stationID = closestStation.stationInfo.YardID;
            }
            record.StationID = stationID;
        }

        public static void Save()
        {
            try
            {
                string configFilePath = Path.Combine(Main.ModEntry.Path, "Config.json");
                var data = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(configFilePath, data);
                return; //true;
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.LogException("Failed to save config", ex);
            }

            return;// false;
        }

        public static bool Load()
        {
            try
            {
                string configFilePath = Path.Combine(Main.ModEntry.Path, "Config.json");
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var data = JsonConvert.DeserializeObject<List<SpawnRecord>>(json);
                        if (data != null)
                        {
                            if (data.Count < 2)
                            {
                                LoadDefaults();
                                return false;
                            }
                            Config = data;
                            ResolveAllRecords();
                            return true;
                        }
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
            Config.Clear();
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