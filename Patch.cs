using DV;
using DV.Logic.Job;
using DV.PointSet;
using DV.ThingTypes;
using DV.Utils;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace dvSlugSpawnsMod
{
    [HarmonyPatch(typeof(StationProceduralJobsController), "TryToGenerateJobs")]
    public static class StationProceduralJobsController_Patch
    {
        private static TrainCarLivery? slugLivery = null;
        private static float? slugLength = null;

        public static void Prefix(StationProceduralJobsController __instance)
        {
            Station station = __instance.stationController.logicStation;
            __instance.StartCoroutine(SpawnSlugCoro(station));
        }

        private static IEnumerator SpawnSlugCoro(Station station)
        {
            string stationId = station.ID;
            var stationSpawnTracks = TrackConfig.Config.Where(sr => sr.StationID == stationId)?.ToArray();
            if (stationSpawnTracks?.Any() is true)
            {
                if (!SpawnCheck()) yield break;
                else yield return null;

                RailTrack[] tracks = SingletonBehaviour<RailTrackRegistryBase>.Instance.OrderedRailtracks;
                foreach (var spawnRecord in stationSpawnTracks)
                {
                    if ((Main.Settings.MaxSlugsNum <= SingletonBehaviour<CarSpawner>.Instance.AllCars.Count(tc => tc.carType == TrainCarType.LocoDE6Slug)) || (!Main.Settings.ForceOccupied && (Random.Range(0f, 2f) > 1))) continue;
                    RailTrack? spawnTrack = tracks[spawnRecord.OrderedRailTrackArrIndex];
                    if (spawnTrack == null) continue;
                    Track logicTrack = spawnTrack.LogicTrack();
                    if ((!spawnRecord.TryOccupied && !Main.Settings.ForceOccupied && !logicTrack.IsFree()) || (logicTrack.length - logicTrack.OccupiedLength - 20f) < slugLength) continue;
                    if (SpawnCarAtBufferStop(spawnTrack, slugLivery!) != null) Main.ModEntry.Logger.Log($"Slug spawned at {spawnTrack} (forced:{Main.Settings.ForceSpawn})");
                    yield return null;
                }
            }
            else yield break;
        }

        public static TrainCar SpawnCarAtBufferStop(RailTrack track, TrainCarLivery carLivery)
        {
            EquiPointSet pointSet = track.GetKinkedPointSet();
            if (pointSet == null || pointSet.points.Length == 0) return null!;

            bool isBufferAtStart = !track.inIsConnected;
            bool isBufferAtEnd = !track.outIsConnected;

            if (!isBufferAtStart && !isBufferAtEnd)
            {
                Main.ModEntry.Logger.Warning($"Track {track.name} does not seem to have a buffer stop (both ends are connected). Defaulting to base game spawning behaviour.");
                List<TrainCar> spawnedCars = SingletonBehaviour<CarSpawner>.Instance.SpawnCarTypesOnTrack([slugLivery], null, track, true, true);
                return spawnedCars.FirstOrDefault();
            }

            TrainCar prefabCar = carLivery.prefab.GetComponent<TrainCar>();
            int startIndex = isBufferAtStart ? 0 : (pointSet.points.Length - 1);
            bool searchForward = isBufferAtStart;
            Bounds carBounds = prefabCar.Bounds;
            EquiPointSet.Point? validPoint = CarSpawner.FindValidPointInOneDirectionForCarStartingFromIndex(pointSet.points, startIndex, carBounds.extents, searchForward);

            if (validPoint == null)
            {
                Main.ModEntry.Logger.Error($"Not enough space to spawn car {carLivery.id} at the end of track {track.LogicTrack().ID.FullDisplayID}.");
                return null!;
            }

            Vector3 spawnPosition = (Vector3)validPoint.Value.position + WorldMover.currentMove;
            Vector3 spawnForward = validPoint.Value.forward;

            return CarSpawner.Instance.SpawnCar(carLivery.prefab, track, spawnPosition, spawnForward);
        }

        private static bool CacheLivery()
        {
            if (slugLivery != null)
            {
                return true;
            }

            slugLivery = Globals.G.Types.Liveries.FirstOrDefault(tcl => tcl.v1 == TrainCarType.LocoDE6Slug);
            if (slugLivery != null)
            {
                slugLength = CarSpawner.Instance.GetTotalCarLiveriesLength([slugLivery], true);
                return true;
            }

            Main.ModEntry.Logger.Error("Slug livery not found, broken game?");
            return false;
        }

        private static bool SpawnCheck()
        {
            GarageType_v2? slugGarage = Globals.G.Types.Garage_to_v2[Garage.DE6_Slug];
            if (slugGarage == null)
            {
                Main.ModEntry.Logger.Error("No Slug garage found, broken map?");
                return false;
            }
            bool garageUnlocked = SingletonBehaviour<LicenseManager>.Instance.IsGarageUnlocked(slugGarage);
            if (!garageUnlocked && !Main.Settings.ForceSpawn)
            {
                Main.ModEntry.Logger.Log("Slug garage still locked, not spawning");
                return false;
            }
            if (!CacheLivery()) return false;

            return true;
        }
    }
}
