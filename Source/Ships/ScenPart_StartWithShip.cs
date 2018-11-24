using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{
    public class ScenPart_StartWithShip : ScenPart
    {
        public List<ShipBase> StartingShips = new List<ShipBase>();

        public ThingDef ShipDef;

        private List<Thing> startingCargo = new List<Thing>();

        public void AddToStartingCargo(Thing newCargo)
        {
            startingCargo.Add(newCargo);
        }

        public ScenPart_StartWithShip()
        {
            shipDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => shipValidator(x));
        }
            


        public void AddToStartingCargo(IEnumerable<Thing> newCargo)
        {
            startingCargo.AddRange(newCargo);
        }
        public override IEnumerable<Thing> PlayerStartingThings()
        {
            return startingCargo;
        }

        public override void Randomize()
        {
            ShipDef = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => shipValidator(x)).RandomElement();
        }

        public override void GenerateIntoMap(Map map)
        {
            if (Find.TickManager.TicksGame < 1000)
            {
                ShipBase newShip = (ShipBase)ThingMaker.MakeThing(ShipDef);
                newShip.SetFaction(Faction.OfPlayer);
                Thing initialFuel = ThingMaker.MakeThing(ShipNamespaceDefOfs.Chemfuel);
                initialFuel.stackCount = 500;
                newShip.refuelableComp.Refuel(new List<Thing>(new Thing[] { initialFuel }));
                StartingShips.Add(newShip);
                DropShipUtility.LoadNewCargoIntoRandomShips(PlayerStartingThings().ToList(), StartingShips);
                DropShipUtility.DropShipGroups(map.Center, map, StartingShips, TravelingShipArrivalAction.EnterMapFriendly);
            }
        }

        private Predicate<ThingDef> shipValidator = delegate (ThingDef t)
        {
            if (t.thingClass == typeof(ShipBase))
            {
                CompProperties_Ship compProps = t.GetCompProperties<CompProperties_Ship>();
                if (compProps != null)
                {
                    if (compProps.CanBeStartingShip)
                    {
                        return true;
                    }
                }
            }
            return false;
        };

        private List<ThingDef> shipDefs = new List<ThingDef>();

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight);
            if (Widgets.ButtonText(scenPartRect, ShipDef.label, true, false, true))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();                

                for (int i=0; i < shipDefs.Count; i++)
                {
                    ThingDef def = shipDefs[i];
                    list.Add(new FloatMenuOption(shipDefs[i].label, delegate
                    {
                        ShipDef = def;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }                    
                
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<ThingDef>(ref ShipDef, "ShipDef");
        }


    }
}
