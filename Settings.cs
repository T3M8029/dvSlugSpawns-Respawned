using UnityModManagerNet;

namespace dvSlugSpawnsMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Ignore garage unlock (force spawn)")]
        public bool ForceSpawn = false;

        [Draw("Always spawn on occupied tracks (caution)")]
        public bool ForceOccupied = false;

        [Draw("Maximum number of slugs present in the world", Min = 1,  Max = 255)]
        public int MaxSlugsNum = 4;

        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }

        public void OnChange() 
        {
            //TrackConfig.Save();
        }
    }
}
