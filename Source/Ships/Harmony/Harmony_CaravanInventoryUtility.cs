using System;
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
                //Log.Error("2");
                __result = new List<Thing>();
                List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
                for (int i = 0; i < pawnsListForReading.Count; i++)
                {
                    Pawn pawn = pawnsListForReading[i];
                    for (int j = 0; j < pawn.inventory.innerContainer.Count; j++)
                    {
                        Thing item = pawn.inventory.innerContainer[j];
                        __result.Add(item);
                    }
                }
                LandedShip landedShip = caravan as LandedShip;

                Predicate<Thing> cargoValidator = delegate (Thing t)
                {
                    Pawn pawn = t as Pawn;
                    if (pawn != null)
                    {
                        if (pawn.IsColonist || pawn.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0)
                        {
                            return false;
                        }
                    }
                    return true;
                };

                if (landedShip != null)
                {
                    __result.AddRange(landedShip.AllLandedShipCargo.Where(x => cargoValidator(x)));
                }            
                return false;
            }
        }
    }
}