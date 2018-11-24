using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_CaravanInventoryUtility
    {
        [HarmonyPatch(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.AllInventoryItems))]
        static class Patch_AllInventoryItems
        {
            [HarmonyPrefix]
            static bool Prefix(ref Caravan caravan, ref List<Thing> __result)
            {
                switch (caravan)
                {
                    case LandedShip ship:
                        
                        __result = new List<Thing>();
                        foreach (var pawn in caravan.PawnsListForReading)
                        {
                            foreach (var item in pawn.inventory.innerContainer)
                            {
                                __result.Add(item);
                            }
                        }
                        __result.AddRange(ship.AllLandedShipCargo.Where(thing =>
                        {
                            switch (thing)
                            {
                                case Pawn pawn:
                                    // TODO part 2 might lead to unexpected results 
                                    if (pawn.IsColonist || pawn.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0)
                                        return false;
                                    break;
                            }

                            return true;
                        }));
                        return false;
                }
                return true;
            }
        }
    }
}