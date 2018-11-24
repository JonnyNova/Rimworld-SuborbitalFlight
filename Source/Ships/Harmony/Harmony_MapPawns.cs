using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_MapPawns
    {
        private static bool IsShipOnMap(Map map)
        {
            return map.listerBuildings.ColonistsHaveBuilding(thing => thing.GetType() == typeof(ShipBase)) 
                   || map.listerThings.AllThings.FirstIndexOf(thing => thing.GetType() == typeof(ShipBase_Traveling)) > 0;
        }
        
        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval))]
        [HarmonyPatch(MethodType.Getter)]
        static class Patch_AnyPawnBlockingMapRemoval
        {
            static void Postfix(ref bool __result, MapPawns __instance, Map ___map)
            {
                if (!__result && IsShipOnMap(___map))
                {
                    __result = true;
                }
            }
        }
    }
}