using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_FactionGenerator
    {
        [HarmonyPatch(typeof(FactionGenerator), nameof(FactionGenerator.GenerateFactionsIntoWorld))]
        static class Patch_GenerateFactionsIntoWorld
        {
            [HarmonyPostfix]
            static void Postfix()
            {
                ShipTracker.GenerateTracker();
            }
        }
    }
}