using Harmony;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_ThingOwner
    {
        [HarmonyPatch(typeof(ThingOwner), "NotifyAddedAndMergedWith")]
        static class Patch_NotifyAddedAndMergedWith
        {
            static void Postfix(ref ThingOwner __instance, Thing item, int mergedCount)
            {
                //Log.Error("9");
                ShipBase ship = __instance.Owner as ShipBase;
                if (ship != null)
                {
                    ship.compShip.NotifyItemAdded(item, mergedCount);
                }
            }
        }

        [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
        static class Patch_NotifyAdded
        {
            [HarmonyPostfix]
            static void Postfix(ThingOwner __instance, Thing item)
            {
                //Log.Error("10");
                ShipBase ship = __instance.Owner as ShipBase;
                if (ship != null)
                {
                    ship.compShip.NotifyItemAdded(item, item.stackCount);
                }
            }
        }
    }
}