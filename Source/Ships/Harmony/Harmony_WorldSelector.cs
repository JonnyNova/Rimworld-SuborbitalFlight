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
                //Log.Error("7");
                LandedShip ship = c as LandedShip;
                if (ship != null)
                {
                    return false;
                }
                return true;
            }
        }
    }
}