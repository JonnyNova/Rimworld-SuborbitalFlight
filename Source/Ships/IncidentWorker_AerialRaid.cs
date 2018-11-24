using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    public class IncidentWorker_AerialRaid : IncidentWorker_RaidEnemy
    {
        private bool UseSappers = false;
        private bool SmartGrid = false;

        private bool Kidnappers(Faction faction)
        {
            if (faction.def.humanlikeFaction)
            {
                return true;
            }
            return false;
        }

        private bool Stealers(Faction faction)
        {
            if (faction.def.humanlikeFaction)
            {
                return true;
            }
            return false;
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            if (parms.points < 700)
            {
                parms.points = Rand.Range(700, 1500);
            }
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            if (base.TryResolveRaidFaction(parms))
            {
                if (DropShipUtility.FactionHasDropShips(parms.faction))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        private void ResolveRaidParmOptions(IncidentParms parms)
        {
            if (parms.raidStrategy.Worker.GetType() == typeof(RaidStrategyWorker_ImmediateAttackSappers))
            {
                UseSappers = true;
            }
            else if (parms.raidStrategy.Worker.GetType() == typeof(RaidStrategyWorker_ImmediateAttackSmart))
            {
                SmartGrid = true;
            }
        }

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return parms.raidStrategy.letterLabelEnemy;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            ResolveRaidPoints(parms);
            if (!TryResolveRaidFaction(parms))
            {
                return false;
            }

            IntVec3 dropCenter;
            dropCenter = DropCellFinder.FindRaidDropCenterDistant(map);

            ResolveRaidStrategy(parms, PawnGroupKindDefOf.Combat);
            ResolveRaidArriveMode(parms);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms, true);
            List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms).ToList<Pawn>();
            if (list.Count == 0)
            {
                Log.Error("Got no pawns spawning raid from parms " + parms);
                return false;
            }
            TargetInfo target = new TargetInfo(dropCenter, map);
            List<ShipBase> ships = DropShipUtility.CreateDropShips(list, parms.faction);

            DropShipUtility.DropShipGroups(dropCenter, map, ships, TravelingShipArrivalAction.EnterMapAssault);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Points = " + parms.points.ToString("F0"));
            foreach (Pawn current2 in list)
            {
                string str = (current2.equipment == null || current2.equipment.Primary == null) ? "unarmed" : current2.equipment.Primary.LabelCap;
                stringBuilder.AppendLine(current2.KindLabel + " - " + str);
            }
            string letterLabel = GetLetterLabel(parms);
            string letterText = GetLetterText(parms, list);
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(list, ref letterLabel, ref letterText, GetRelatedPawnsInfoLetterText(parms), true);
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, GetLetterDef(), target, defaultPawnGroupMakerParms.faction, stringBuilder.ToString());
            ResolveRaidParmOptions(parms);
            Lord lord = LordMaker.MakeNewLord(parms.faction, new LordJob_AerialAssault(ships, parms.faction, Kidnappers(parms.faction), true, UseSappers, SmartGrid, Stealers(parms.faction)), map, list);
            //Lord lord = LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(parms.faction, true, true, true, true, true), map, list);
            map.avoidGrid.Regenerate();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
            if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i];
                    if (pawn.apparel.WornApparel.Any((Apparel ap) => ap is ShieldBelt))
                    {
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
                        break;
                    }
                }
            }
            if (DebugViewSettings.drawStealDebug && parms.faction.HostileTo(Faction.OfPlayer))
            {
                Log.Message(string.Concat(new object[]
                {
            "Market value threshold to start stealing: ",
            StealAIUtility.StartStealingMarketValueThreshold(lord),
            " (colony wealth = ",
            map.wealthWatcher.WealthTotal,
            ")"
                }));
            }
            return true;
        }
    }
}
