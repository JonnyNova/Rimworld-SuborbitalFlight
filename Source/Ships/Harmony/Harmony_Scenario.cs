using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace OHUShips.Harmony
{
    public class Harmony_Scenario
    {
        [HarmonyPatch(typeof(Scenario), nameof(Scenario.GenerateIntoMap))]
        static class Patch_GenerateIntoMap
        {
            [HarmonyPrefix]
            static void Prefix(Map map)
            {
                //Log.Error("8");
                if (Find.GameInitData == null)
                {
                    return;
                }
                else
                {
                    ScenPart_StartWithShip scenPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_StartWithShip) as ScenPart_StartWithShip;
                    if (scenPart != null)
                    {
                        List<List<Thing>> list = new List<List<Thing>>();
                        foreach (Pawn current in Find.GameInitData.startingAndOptionalPawns)
                        {
                            list.Add(new List<Thing>
                            {
                                current
                            });
                        }
                        List<Thing> list2 = new List<Thing>();
                        foreach (ScenPart current2 in Find.Scenario.AllParts)
                        {
                            list2.AddRange(current2.PlayerStartingThings());
                        }
                        int num = 0;
                        foreach (Thing current3 in list2)
                        {
                            if (current3.def.CanHaveFaction)
                            {
                                current3.SetFactionDirect(Faction.OfPlayer);
                            }
                            list[num].Add(current3);
                            num++;
                            if (num >= list.Count)
                            {
                                num = 0;
                            }
                        }
                        foreach (List<Thing> current in list)
                        {
                            scenPart.AddToStartingCargo(current);
                        }
                        ScenPart_PlayerPawnsArriveMethod arrivalPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_PlayerPawnsArriveMethod) as ScenPart_PlayerPawnsArriveMethod;
                        if (arrivalPart != null)
                        {
                            Find.Scenario.RemovePart(arrivalPart);
                        }
                    }
                }
            }
        }
    }
}