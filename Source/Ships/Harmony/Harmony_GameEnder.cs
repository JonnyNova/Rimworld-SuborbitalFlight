using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_GameEnder
    {
        [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver))]
        static class Patch_CheckOrUpdateGameOver
        {
            [HarmonyPostfix]
            static void Postfix()
            {
                //Log.Error("5");
                List<TravelingShips> travelingShips = Find.WorldObjects.AllWorldObjects.FindAll(x => x is TravelingShips).Cast<TravelingShips>().ToList();
                for (int i=0; i < travelingShips.Count; i++)
                {
                    TravelingShips ship = travelingShips[i];
                    if (ship.containsColonists)
                    {
                        Find.GameEnder.gameEnding = false;
                    }
                }
            }
        }
    }
}