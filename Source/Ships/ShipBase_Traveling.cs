using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OHUShips
{
    public class ShipBase_Traveling : ThingWithComps
    {
        public PawnsArrivalModeDef pawnArriveMode;
        public int destinationTile = -1;
        private bool alreadyLeft;
        public bool leavingForTarget;
        private bool launchAsFleet;
        private bool dropPawnsOnTochdown = true;
        private bool dropItemsOnTouchdown = false;
        private TravelingShipArrivalAction arrivalAction;
        public ShipBase containingShip;

        public IntVec3 destinationCell = IntVec3.Invalid;

        public int fleetID = -1;

        public ShipBase_Traveling()
        {
            Rotation = Rot4.North;
            containingShip = new ShipBase();
        }

        public ShipBase_Traveling(ShipBase ship)
        {
            Rotation = Rot4.North;
            containingShip = ship;
            leavingForTarget = false;
        }

        public ShipBase_Traveling(ShipBase ship, bool launchAsFleet = false, TravelingShipArrivalAction arrivalAction = TravelingShipArrivalAction.StayOnWorldMap)
        {
            containingShip = ship;
            def = ship.compShip.sProps.LeavingShipDef;
            def.size = ship.def.size;
            def.graphicData = ship.def.graphicData;
            this.launchAsFleet = launchAsFleet;
            Rotation = ship.Rotation;

            this.arrivalAction = arrivalAction;
        }


        public ShipBase_Traveling(ShipBase ship, RimWorld.Planet.GlobalTargetInfo target, PawnsArrivalModeDef arriveMode, TravelingShipArrivalAction arrivalAction = TravelingShipArrivalAction.StayOnWorldMap, bool leavingForTarget = true)
        {
            containingShip = ship;
            def = ship.compShip.sProps.LeavingShipDef;
            def.size = ship.def.size;
            def.graphicData = ship.def.graphicData;
            destinationTile = target.Tile;
            destinationCell = target.Cell;
            pawnArriveMode = arriveMode;
            this.leavingForTarget = leavingForTarget;
            Rotation = ship.Rotation;
            this.arrivalAction = arrivalAction;
        }
        
        public override void Draw()
        {
            containingShip.DrawAt(DropShipUtility.DrawPosAt(containingShip, containingShip.drawTickOffset, this));
            foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> current in containingShip.installedTurrets)
            {
                if (current.Value != null)
                {
                    current.Value.Draw();
                }
            }
            DropShipUtility.DrawDropSpotShadow(containingShip, containingShip.drawTickOffset, this);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            containingShip.compShip.TryRemoveLord(map);
        }

        public override void Tick()
        {
            base.Tick();
            if (containingShip.shipState == ShipState.Incoming)
            {
                containingShip.drawTickOffset--;
                if (containingShip.drawTickOffset <= 0)
                {
                    ShipImpact();
                }
                containingShip.refuelableComp.ConsumeFuel(containingShip.refuelableComp.Props.fuelConsumptionRate / 60f);
            }
            
            if (containingShip.shipState == ShipState.Outgoing)
            {
                containingShip.drawTickOffset++;
                if (containingShip.drawTickOffset >= containingShip.compShip.sProps.TicksToDespawn)
                {
                    if (leavingForTarget)
                    {
                        GroupLeftMap();
                    }
                    else
                    {
                        List<Pawn> pawns = DropShipUtility.AllPawnsInShip(containingShip);
                        for (int i=0; i < pawns.Count; i++)
                        {
                            Find.WorldPawns.PassToWorld(pawns[i]);
                        }                        
                    }
                }
            }     
        }

        private void ShipImpact()
        {
       //     Log.Message("ShipImpact at " + Position.ToString() + " with truecenter" + Gen.TrueCenter(this).ToString() + " and ticks: " + containingShip.drawTickOffset.ToString());
            containingShip.shipState = ShipState.Stationary;

            for (int i = 0; i < 6; i++)
            {
                Vector3 loc = base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
                MoteMaker.ThrowDustPuff(loc, base.Map, 1.2f);
            }
            MoteMaker.ThrowLightningGlow(base.Position.ToVector3Shifted(), base.Map, 2f);
            RoofDef roof = Position.GetRoof(Map);
            if (roof != null)
            {
                if (!roof.soundPunchThrough.NullOrUndefined())
                {
                    roof.soundPunchThrough.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                }
                if (roof.filthLeaving != null)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        FilthMaker.MakeFilth(base.Position, base.Map, roof.filthLeaving, 1);
                    }
                }
            }

            var position = Position;
            var map = Map;
            DeSpawn();
            GenSpawn.Spawn(containingShip, position, map, containingShip.Rotation);
            containingShip.ShipUnload(false, dropPawnsOnTochdown, dropItemsOnTouchdown);
        }

        private void GroupLeftMap()
        {            
            if (destinationTile < 0)
            {
                Log.Error("Drop pod left the map, but its destination tile is " + destinationTile);
                Destroy(DestroyMode.Vanish);
                return;
            }
            TravelingShips travelingShips = (TravelingShips)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.TravelingSuborbitalShip);
            travelingShips.Tile = base.Map.Tile;
            travelingShips.SetFaction(Faction);
            travelingShips.destinationTile = destinationTile;
            travelingShips.destinationCell = destinationCell;
            travelingShips.arriveMode = pawnArriveMode;
            travelingShips.arrivalAction = arrivalAction;
            Find.WorldObjects.Add(travelingShips);
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (t != this)
                {
                    if (t is ShipBase_Traveling)
                    {
                        ShipBase_Traveling ship = (ShipBase_Traveling)t;
                        if (ship.containingShip.shipState == ShipState.Outgoing)
                        {
                            return true;
                        }
                    }
                }
                return false;
            };
            List<Thing> tmpleavingShips = base.Map.listerThings.AllThings.FindAll(x => predicate(x));
            for (int i = 0; i < tmpleavingShips.Count; i++)
            {
                ShipBase_Traveling dropPodLeaving = tmpleavingShips[i] as ShipBase_Traveling;
                if (dropPodLeaving != null && dropPodLeaving.fleetID == fleetID)
                {
                    dropPodLeaving.alreadyLeft = true;
                    travelingShips.AddShip(dropPodLeaving.containingShip, true);
                    dropPodLeaving.Destroy(DestroyMode.Vanish);
                }
            }
            travelingShips.AddShip(containingShip, true);
            travelingShips.SetFaction(containingShip.Faction);
            
            Destroy(DestroyMode.Vanish);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref fleetID, "fleetID", 0, false);
            Scribe_Values.Look<int>(ref destinationTile, "destinationTile", 0, false);
            Scribe_Values.Look<IntVec3>(ref destinationCell, "destinationCell", default(IntVec3), false);
            Scribe_Values.Look<TravelingShipArrivalAction>(ref arrivalAction, "arrivalAction", TravelingShipArrivalAction.StayOnWorldMap, false);
            Scribe_Values.Look<PawnsArrivalModeDef>(ref pawnArriveMode, "pawnArriveMode", PawnsArrivalModeDefOf.CenterDrop, false);

            Scribe_Values.Look<bool>(ref leavingForTarget, "leavingForTarget", true, false);
            Scribe_Values.Look<bool>(ref alreadyLeft, "alreadyLeft", false, false);
            Scribe_Deep.Look<ShipBase>(ref containingShip, "containingShip", new object[0]);
        }
    }
}
