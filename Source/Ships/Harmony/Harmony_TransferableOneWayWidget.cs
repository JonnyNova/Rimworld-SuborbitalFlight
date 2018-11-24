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
                IEnumerable<TransferableOneWay> transferables)
            {
                foreach (var transferable in transferables)
                {
                    Dialog_LoadShipCargo.RemoveExistingTransferable(transferable, Find.CurrentMap);
                }
            }
        }
    }
}