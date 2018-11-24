using Harmony;
using RimWorld.Planet;

namespace OHUShips.Harmony
{
    public class Harmony_WorldSelector
    {
        [HarmonyPatch(typeof(WorldSelector), "AutoOrderToTileNow")]
        static class Patch_AutoOrderToTileNow
        {
            static bool Prefix(Caravan c, int tile)
            {
                switch (c)
                {
                    case LandedShip ship: 
                        return false;
                    default: 
                        return true;
                }
            }
        }
    }
}