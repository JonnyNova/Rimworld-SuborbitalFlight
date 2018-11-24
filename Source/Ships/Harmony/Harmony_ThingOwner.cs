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
                switch (__instance.Owner)
                {
                    case ShipBase ship:
                        ship.compShip.NotifyItemAdded(item, mergedCount);
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
        static class Patch_NotifyAdded
        {
            [HarmonyPostfix]
            static void Postfix(ThingOwner __instance, Thing item)
            {
                switch (__instance.Owner)
                {
                    case ShipBase ship:
                        ship.compShip.NotifyItemAdded(item, item.stackCount);
                        break;
                }
            }
        }
    }
}