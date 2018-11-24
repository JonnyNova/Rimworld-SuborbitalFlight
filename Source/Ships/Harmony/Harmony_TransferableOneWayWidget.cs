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
            static void Prefix(
                TransferableOneWayWidget __instance, 
                string title,
                IEnumerable<TransferableOneWay> transferables)
            {
                //Log.Error("4");
                List<TransferableOneWay> tmp = transferables.ToList();
                for (int i = 0; i < tmp.Count; i++)
                {
                    Dialog_LoadShipCargo.RemoveExistingTransferable(tmp[i], Find.CurrentMap);
                    //tmp[i].AdjustTo(tmp[i].GetMinimum());
                }
            }
        }
    }
}