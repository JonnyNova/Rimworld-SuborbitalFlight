using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;

namespace OHUShips
{
    public class TravelingShips : WorldObject
    {
        private HashSet<ShipBase> ships = new HashSet<ShipBase>();

        private const float TravelSpeed = 0.00025f;

        public int destinationTile = -1;

        public IntVec3 destinationCell = IntVec3.Invalid;

        public PawnsArrivalModeDef arriveMode;

        public TravelingShipArrivalAction arrivalAction;

        private bool arrived;

        private int initialTile = -1;

        public IntVec3 launchCell = IntVec3.Invalid;

        private float traveledPct;
        
        public Material cachedMat;
        
        private float maxTravelingSpeed = -1;
        
        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                {
                    var first = ships.First();
                    cachedMat = MaterialPool.MatFrom(first.def.graphicData.texPath, ShaderDatabase.WorldOverlayCutout, first.DrawColor, WorldMaterials.WorldObjectRenderQueue);
                }
                return cachedMat;
            }
        }
        
        public override Texture2D ExpandingIcon
        {
            get
            {
                if (ships.Count > 1)
                {
                    return DropShipUtility.movingFleet;
                }
                return DropShipUtility.movingShip;
            }
        }

        private Vector3 Start
        {
            get
            {
                return Find.WorldGrid.GetTileCenter(initialTile);
            }
        }

        private Vector3 End
        {
            get
            {
                return Find.WorldGrid.GetTileCenter(destinationTile);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return Vector3.Slerp(Start, End, traveledPct);
            }
        }

        private bool isSingularShip
        {
            get
            {
                if (ships.Count == 1)
                {
                    return true;
                }
                return false;
            }
        }

        public float MaxTravelingSpeed
        {
            get
            {
                if (maxTravelingSpeed == -1)
                {
                    List<float> speedFactors = new List<float>();
                    foreach (ShipBase ship in ships)
                    {
                        speedFactors.Add(ship.compShip.sProps.WorldMapTravelSpeedFactor);
                    }
                    float chosenFactor = Mathf.Min(speedFactors.ToArray());
                    maxTravelingSpeed = chosenFactor * 0.0000416f;
                }
                
                return maxTravelingSpeed;
            }
        }

        private float TraveledPctStepPerTick
        {
            get
            {
                Vector3 start = Start;
                Vector3 end = End;
                if (start == end)
                {
                    return 1f;
                }
                float num = GenMath.SphericalDistance(start.normalized, end.normalized);
                if (num == 0f)
                {
                    return 1f;
                }
                return MaxTravelingSpeed / num;
            }
        }

        public bool PodsHaveAnyFreeColonist
        {
            get
            {
                foreach (var ship in ships)
                {
                    ThingOwner innerContainer = ship.GetDirectlyHeldThings();
                    foreach (var thing in innerContainer)
                    {
                        switch (thing)
                        {
                            case Pawn pawn:
                                if (pawn.IsColonist && pawn.HostFaction == null) return true;
                                break;
                        }
                    }
                }
                return false;
            }
        }

        public IEnumerable<Pawn> Pawns
        {
            get
            {
                foreach(var ship in ships)
                {
                    foreach (var thing in ship.GetDirectlyHeldThings())
                    {
                        switch (thing)
                        {
                            case Pawn pawn:
                                yield return pawn;
                                break;
                        }
                    }
                }
            }
        }

        public bool containsColonists
        {
            get
            {
                List<Pawn> pawns = Pawns.ToList();
                for (int i=0; i < pawns.Count; i++)
                {
                    if (pawns[i].IsColonist)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public IEnumerable<ShipBase> Ships => ships;

        public ShipBase LeadShip => ships.First();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<ShipBase>(ref ships, "ships", LookMode.Deep);
            Scribe_Values.Look<int>(ref destinationTile, "destinationTile", 0, false);
            Scribe_Values.Look<IntVec3>(ref destinationCell, "destinationCell", default(IntVec3), false);
            Scribe_Values.Look<bool>(ref arrived, "arrived", false, false);
            Scribe_Values.Look<int>(ref initialTile, "initialTile", 0, false);
            Scribe_Values.Look<float>(ref traveledPct, "traveledPct", 0f, false);
            Scribe_Values.Look<TravelingShipArrivalAction>(ref arrivalAction, "arrivalAction", TravelingShipArrivalAction.StayOnWorldMap, false);            
        }

        public override void PostAdd()
        {
            base.PostAdd();
            initialTile = base.Tile;
        }

        public override void Tick()
        {
            base.Tick();
            BurnFuel();
            traveledPct += TraveledPctStepPerTick;
            if (traveledPct >= 1f)
            {                
                traveledPct = 1f;
                Arrived();
            }
        }

        public void Remove(ShipBase ship)
        {
            ships.Remove(ship);
        }

        private void BurnFuel()
        {
            foreach (ShipBase ship in ships)
            {
                ship.refuelableComp.ConsumeFuel(ship.refuelableComp.Props.fuelConsumptionRate / 60f);
                if (!ship.refuelableComp.HasFuel && !ship.Destroyed)
                {
                    Messages.Message("ShipOutOfFuelCrash".Translate(ship.ShipNick), MessageTypeDefOf.ThreatBig);
                    ship.Destroy();
                    DropShipUtility.currentShipTracker.AllWorldShips.Remove(ship);
                }
            }

            // TODO why is this here?
//            ships.RemoveAll(x => x.Destroyed);
        }

        public void AddShip(ShipBase ship, bool justLeftTheMap)
        {
            if (!ships.Contains(ship))
            {
                ships.Add(ship);
            }
        }

        public bool ContainsPawn(Pawn p)
        {
            foreach (var ship in ships)
            {
                if (ship.GetDirectlyHeldThings().Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        private void Arrived()
        {
            if (arrived)
            {
                return;
            }

            arrived = true;
            if (TravelingShipsUtility.TryAddToLandedFleet(this, destinationTile))
            {
                return;
            }
            if (arrivalAction == TravelingShipArrivalAction.BombingRun)
            {
                MapParent parent = Find.World.worldObjects.MapParentAt(destinationTile);
                if (parent != null)
                {
                    Messages.Message("MessageBombedSettlement".Translate(parent.ToString(), parent.Faction.Name), parent, MessageTypeDefOf.NeutralEvent);
                    Find.World.worldObjects.Remove(parent);
                }
                SwitchOriginToDest();

                //TravelingShips travelingShips = (TravelingShips)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.TravelingSuborbitalShip);
                //travelingShips.ships.AddRange(ships);
                //travelingShips.Tile = destinationTile;
                //travelingShips.SetFaction(Faction.OfPlayer);
                //travelingShips.destinationTile = initialTile;
                //travelingShips.destinationCell = launchCell;
                //travelingShips.arriveMode = arriveMode;
                //travelingShips.arrivalAction = TravelingShipArrivalAction.EnterMapFriendly;
                //Find.WorldObjects.Add(travelingShips);
                //Find.WorldObjects.Remove(this);
            }
            else
            {
                Map map = Current.Game.FindMap(destinationTile);
                if (map != null)
                {
                    SpawnShipsInMap(map, null);
                }
                else if (!LandedShipHasCaravanOwner)
                {
                    foreach (var ship in ships)
                    {
                        ship.GetDirectlyHeldThings().ClearAndDestroyContentsOrPassToWorld(DestroyMode.Vanish);
                    }
                    RemoveAllPods();
                    Find.WorldObjects.Remove(this);
                    Messages.Message("MessageTransportPodsArrivedAndLost".Translate(), new GlobalTargetInfo(destinationTile), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    var factionBase = Find.WorldObjects.Settlements.Find((x) => x.Tile == destinationTile);
                    if (factionBase != null && factionBase.Faction != Faction.OfPlayer && arrivalAction != TravelingShipArrivalAction.StayOnWorldMap)
                    {
                        LongEventHandler.QueueLongEvent(delegate
                        {
                            Map map2 = GetOrGenerateMapUtility.GetOrGenerateMap(factionBase.Tile, Find.World.info.initialMapSize, null); ;
                            
                            string extraMessagePart = null;
                            if (arrivalAction == TravelingShipArrivalAction.EnterMapAssault && !factionBase.Faction.HostileTo(Faction.OfPlayer))
                            {
                                factionBase.Faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile, true);
                                extraMessagePart = "MessageTransportPodsArrived_BecameHostile".Translate(factionBase.Faction.Name).CapitalizeFirst();
                            }
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                            Current.Game.CurrentMap = map2;
                            Find.CameraDriver.JumpToCurrentMapLoc(map2.Center);
                            SpawnShipsInMap(map2, extraMessagePart);
                        }, "GeneratingMapForNewEncounter", false, null);
                    }
                    else
                    {
                        SpawnCaravanAtDestinationTile();
                    }
                }
            }
        }

        private void SpawnCaravanAtDestinationTile()
        {
            TravelingShipsUtility.tmpPawns.Clear();
            foreach (var ship in ships)
            {
                ThingOwner innerContainer = ship.GetDirectlyHeldThings();
            //    Log.Message("SpawningCaravan");
            //    TravelingShipsUtility.MakepawnInfos(innerContainer);
                for (int j = 0; j < innerContainer.Count; j++)
                {
                    Pawn pawn = innerContainer[j] as Pawn;
                    if (pawn != null)
                    {
                        TravelingShipsUtility.tmpPawns.Add(pawn);
                    }
                }
            }
            int startingTile;
            if (!GenWorldClosest.TryFindClosestPassableTile(destinationTile, out startingTile))
            {
                startingTile = destinationTile;
            }
            
            LandedShip landedShip = TravelingShipsUtility.MakeLandedShip(this, Faction, startingTile, true);
            RemoveAllPods();
            Find.WorldObjects.Remove(this);
            
            Messages.Message("MessageShipsArrived".Translate(), landedShip, MessageTypeDefOf.NeutralEvent);
        }

        public bool IsPlayerControlled
        {
            get
            {
                return base.Faction == Faction.OfPlayer;
            }
        }        

        private void SpawnShipsInMap(Map map, string extraMessagePart = null)
        {
            RemoveAllPawnsFromWorldPawns();
            IntVec3 intVec;
            if (destinationCell.IsValid && destinationCell.InBounds(map))
            {
                intVec = destinationCell;
            }
            else if (arriveMode == PawnsArrivalModeDefOf.CenterDrop)
            {
                intVec = DropCellFinder.FindRaidDropCenterDistant(map);
            }
            else
            {
                if (arriveMode == PawnsArrivalModeDefOf.EdgeDrop)
                {
                    Log.Warning("Unsupported arrive mode " + arriveMode);
                }
                Log.Message("Invalid Cell");
                intVec = DropCellFinder.FindRaidDropCenterDistant(map);
            }

            string text = "MessageShipsArrived".Translate();
            if (extraMessagePart != null)
            {
                text = text + " " + extraMessagePart;
            }
            DropShipUtility.DropShipGroups(intVec, map, ships, arrivalAction, isSingularShip);
            Messages.Message(text, new TargetInfo(intVec, map, false), MessageTypeDefOf.NeutralEvent);
            RemoveAllPods();
            Find.WorldObjects.Remove(this);
        }

        private bool LandedShipHasCaravanOwner
        {
            get
            {
                foreach (var ship in ships)
                {
                    ThingOwner innerContainer = ship.GetDirectlyHeldThings();
                    for (int j = 0; j < innerContainer.Count; j++)
                    {
                        Pawn pawn = innerContainer[j] as Pawn;
                        if (pawn != null)
                        {
                            if (CaravanUtility.IsOwner(pawn, Faction))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        private void RemoveAllPawnsFromWorldPawns()
        {
            foreach (var ship in ships)
            {
                ThingOwner innerContainer = ship.GetDirectlyHeldThings();
                for (int j = 0; j < innerContainer.Count; j++)
                {
                    Pawn pawn = innerContainer[j] as Pawn;
                    if (pawn != null && pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.RemovePawn(pawn);
                    }
                }
            }
        }

        private void RemoveAllPods()
        {
            ships.Clear();
        }

        public void SwitchOriginToDest()
        {
            traveledPct = 0f;
            arrived = false;
            arrivalAction = TravelingShipArrivalAction.EnterMapFriendly;

            int bufferTile = destinationTile;

            destinationCell = launchCell;
            destinationTile = initialTile;

            initialTile = bufferTile;
            launchCell = IntVec3.Zero;
        }
    }
}
