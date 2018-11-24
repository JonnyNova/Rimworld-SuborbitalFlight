using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_TransferableOneWayWidget
    {
        [HarmonyPatch(typeof(TransferableOneWayWidget), nameof(TransferableOneWayWidget.AddSection))]
        static class Patch_AddSection
        {
            [HarmonyPrefix]
            static void Prefix(
                TransferableOneWayWidget __instance, 
                string title,
                ref IEnumerable<TransferableOneWay> transferables)
            {
                var copy = transferables.ToList();
                foreach (var transferable in copy)
                {
                    Dialog_LoadShipCargo.RemoveExistingTransferableItems(transferable, Find.CurrentMap);
                }
                transferables = copy;
            }
        }
    }
}