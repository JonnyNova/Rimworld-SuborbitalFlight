using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_FactionGenerator
    {
        [HarmonyPatch(typeof(FactionGenerator), nameof(FactionGenerator.GenerateFactionsIntoWorld))]
        public static class Patch_GenerateFactionsIntoWorld
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                //Log.Error("6");
                Log.Message("GeneratingShipTracker");
                ShipTracker shipTracker = (ShipTracker)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipTracker);
                int tile = 0;
                while (!(Find.WorldObjects.AnyWorldObjectAt(tile) || Find.WorldGrid[tile].biome == BiomeDefOf.Ocean))
                {
                    tile = Rand.Range(0, Find.WorldGrid.TilesCount);
                }
                shipTracker.Tile = tile;
                Find.WorldObjects.Add(shipTracker);
            }
        }
    }
}