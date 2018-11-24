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
                var colonistsTraveling = Find.WorldObjects.AllWorldObjects
                    .FindAll(x => x is TravelingShips)
                    .Cast<TravelingShips>()
                    .Any(ship => ship.containsColonists);
                if(colonistsTraveling)
                    Find.GameEnder.gameEnding = false;
            }
        }
    }
}