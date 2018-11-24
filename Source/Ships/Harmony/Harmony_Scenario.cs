using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_Scenario
    {
        private static IEnumerable<Thing> StartingShipContents()
        {
            foreach (var pawn in Find.GameInitData.startingAndOptionalPawns)
            {
                yield return pawn;
            }

            foreach (var scenPart in Find.Scenario.AllParts)
            {
                foreach (var thing in scenPart.PlayerStartingThings())
                {
                    yield return thing;
                }
            }
        }

        [HarmonyPatch(typeof(Scenario), nameof(Scenario.GenerateIntoMap))]
        static class Patch_GenerateIntoMap
        {
            [HarmonyPrefix]
            static void Prefix(Map map)
            {
                if (Find.GameInitData == null) return;
                var scenPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_StartWithShip) as ScenPart_StartWithShip;
                if (scenPart == null) return;
                scenPart.AddToStartingCargo(StartingShipContents());
                Find.Scenario.AllParts
                    .Where(x => x is ScenPart_PlayerPawnsArriveMethod)
                    .Do(part => Find.Scenario.RemovePart(part));
            }
        }
    }
}