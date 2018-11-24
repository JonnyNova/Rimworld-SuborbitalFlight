using System.Collections.Generic;
using Harmony;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_MapPawns
    {
        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval))]
        [HarmonyPatch(MethodType.Getter)]
        static class Patch_AnyPawnBlockingMapRemoval
        {
            static void Postfix(ref bool __result, MapPawns __instance)
            {
                //Log.Error("1");
                if (!__result)
                {
                    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                    if (map != null)
                    {
                        List<Thing> list = map.listerThings.AllThings.FindAll(x => x is ShipBase_Traveling || x is ShipBase);
                        if (list.Count > 0)
                        {
                            __result = true;
                        }
                    }
                }
            }
        }
    }
}