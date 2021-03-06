﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI.Group;

namespace OHUShips
{
    public class LordJob_AerialAssault : LordJob_AssaultColony
    {
        public List<ShipBase> ships;

        public  LordJob_AerialAssault(List<ShipBase> ships,  Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool sappers = false, bool useAvoidGridSmart = false, bool canSteal = true) : base(assaulterFaction, canKidnap, canTimeoutOrFlee, sappers, useAvoidGridSmart, canSteal)
        {
            this.ships = ships;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = base.CreateGraph();
            List<Transition> leaveTransitions = graph.transitions.FindAll(x => x.target.GetType() == typeof(LordToil_ExitMapAndEscortCarriers));
            for (int i=0; i < leaveTransitions.Count; i++)
            {
                LordToil_LeaveInShip lordToil = new LordToil_LeaveInShip();
                leaveTransitions[i].target = lordToil;

                graph.AddToil(lordToil);
                Transition transition = new Transition(leaveTransitions[i].target, new LordToil_ExitMapAndEscortCarriers());
                transition.AddTrigger(new Trigger_Custom((TriggerSignal x) => !ships.Any(y => y.Map == Map)));
                graph.transitions.Add(transition);
            }
            Transition stealTransitions = graph.transitions.FirstOrDefault(x => x.target.GetType() == typeof(LordToil_StealCover));
            LordToil_StealForShip stealToil = new LordToil_StealForShip();
            graph.AddToil(stealToil);
            stealTransitions.target = stealToil;

            return graph;

        }

    }
}
