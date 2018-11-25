using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FrontierDevelopments.SuborbitalFlight.Module;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace OHUShips
{
    public class ShipBase : Building, IThingHolder
    {
        public bool FirstSpawned = true;

        public List<Building_ShipTurret> assignedTurrets = new List<Building_ShipTurret>();
        public Dictionary<WeaponSystem, bool> assignedSystemsToModify = new Dictionary<WeaponSystem, bool>();
        public List<WeaponSystemShipBomb> loadedBombs = new List<WeaponSystemShipBomb>();

        public Dictionary<ShipWeaponSlot, Building_ShipTurret> installedTurrets = new Dictionary<ShipWeaponSlot, Building_ShipTurret>();
        public Dictionary<ShipWeaponSlot, WeaponSystemShipBomb> Payload = new Dictionary<ShipWeaponSlot, WeaponSystemShipBomb>();
        public Dictionary<ShipWeaponSlot, Thing> weaponsToInstall = new Dictionary<ShipWeaponSlot, Thing>();
        public Dictionary<ShipWeaponSlot, Thing> weaponsToUninstall = new Dictionary<ShipWeaponSlot, Thing>();
                        
        public bool shouldSpawnTurrets = false;
        //public bool shouldDeepSave = true;

        public List<Pawn> assignedNewPawns = new List<Pawn>();

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }
                
        public override Graphic Graphic
        {
            get
            {
                return GraphicDatabase.Get<Graphic_Single>(def.graphicData.texPath, ShaderDatabase.LoadShader(def.graphicData.shaderType.shaderPath), def.graphicData.drawSize, DrawColor, DrawColorTwo);
            }
        }

        public string ShipNick = "Ship";
        
        public ShipState shipState = ShipState.Stationary;

        private ThingOwner innerContainer;

        protected CompShip compShipCached;

        public CompPassengerModule PassengerModule => this.TryGetComp<CompPassengerModule>();

        public IEnumerable<Pawn> Passengers => PassengerModule?.Passengers ?? new List<Pawn>();

        public int PassengerCapacity => PassengerModule?.Capacity ?? 0;
        
        public CompShip compShip
        {
            get
            {
                if (compShipCached == null)
                {
                    compShipCached = this.TryGetComp<CompShip>();
                }
                return compShipCached;
            }
        }

        protected CompRefuelable refuelableCompCached;

        public CompRefuelable refuelableComp
        {
            get
            {
                if (refuelableCompCached == null)
                {
                    refuelableCompCached = this.TryGetComp<CompRefuelable>();
                }
                return refuelableCompCached;
            }
        }

        public int drawTickOffset = 0;

        private const int maxTimeToWait = 3000;        

        private int timeWaited = 0;
        
        private int timeToLiftoff = 50;

        private bool NoneLeftBehind = false;

        private bool ShouldWait = false;

        private bool isTargeting = false;
        
        public bool keepShipReference;
                
        public int fleetID = -1;

        public bool LaunchAsFleet;

        public bool performBombingRun;

        public Map ParkingMap;

        public IntVec3 ParkingPosition;

        private LandedShip landedShipCached;
        
        public bool ShouldSpawnFueled;

        public bool holdFire = true;
                
        public bool ActivatedLaunchSequence;

        private bool DeepsaveTurrets = false;

        public LandedShip parentLandedShip
        {
            get
            {
                if (landedShipCached == null)
                {

                    foreach (LandedShip ship in Find.WorldObjects.AllWorldObjects.FindAll(x => x is LandedShip))
                    {
                        if (ship.ships.Contains(this))
                        {
                            landedShipCached = ship;
                        }
                    }
                }
                return landedShipCached;
            }
        }

        public bool pilotPresent => PassengerModule?.HasPilot ?? false;

        public ShipBase()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public ShipBase(bool isIncoming = false, bool shouldSpawnRefueled = false)
        {
            if (isIncoming)
            {
                shipState = ShipState.Incoming;
                drawTickOffset = compShip.sProps.TicksToImpact;
            }
            else
            {
                shipState = ShipState.Stationary;
            }
            ShouldSpawnFueled = shouldSpawnRefueled;
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public override void PostMake()
        {
            base.PostMake();
            InitiateShipProperties();
        }
        
        public int MaxLaunchDistanceEverPossible(bool LaunchAsFleet, bool includeReturnFlight = false)
        {
            if (LaunchAsFleet && fleetID != -1)
            {
                List<ShipBase> fleetShips = DropShipUtility.currentShipTracker.ShipsInFleet(fleetID);

                ShipBase lowest = fleetShips.Aggregate((curMin, x) => (curMin == null || x.MaxLaunchDistanceEverPossible(false) < curMin.MaxLaunchDistanceEverPossible(false) ? x : curMin));
                return (int)((lowest.MaxShipFlightTicks * lowest.compShip.sProps.WorldMapTravelSpeedFactor * 0.0000416f) / 0.005F);
            }
            return (int)((MaxShipFlightTicks * compShip.sProps.WorldMapTravelSpeedFactor * 0.0000416f)/ 0.005F);
        }

        public int MaxShipFlightTicks
        {
            get
            {
                float consumption = refuelableComp.Props.fuelConsumptionRate;
                float fuel = refuelableComp.Fuel;
                return (int)((fuel / consumption) * 60) - MapFlightTicks;
            }

        }

        private int MapFlightTicks
        {
            get
            {
                return compShip.sProps.TicksToImpact + compShip.sProps.TicksToDespawn;
            }
        }       
        

        public bool ReadyForTakeoff
        {
            get
            {
                return pilotPresent && refuelableComp.HasFuel;
            }
        }


        private void InitiateShipProperties()
        {
            DropShipUtility.currentShipTracker.AllWorldShips.Add(this);
            ShipNick = NameGenerator.GenerateName(RulePackDef.Named("NamerShipGeneric"));
            compShipCached = this.TryGetComp<CompShip>();
            if (compShip == null)
            {
                Log.Error("DropShip is missing " + nameof(CompProperties_Ship) + "/n Defaulting.");
                compShipCached = new CompShip();
                drawTickOffset = compShip.sProps.TicksToImpact;
            }
            if (installedTurrets.Count == 0)
            {
                InitiateInstalledTurrets();
            }            
        }

        private void InitiateInstalledTurrets()
        {
            foreach (ShipWeaponSlot current in compShip.sProps.weaponSlots)
            {
                if (current.slotType == WeaponSystemType.LightCaliber)
                {

                    installedTurrets.Add(current, null);
                }
                if (current.slotType == WeaponSystemType.Bombing)
                {
                    Payload.Add(current, null);
                }
                if (assignedTurrets.Count > 0)
                {
                    Building_ShipTurret turret = assignedTurrets.Find(x => x.assignedSlotName == current.SlotName);
                    if (turret != null)
                    {
                        turret.AssignParentShip(this);
                        installedTurrets[current] = turret;
                    }
                }
                else
                {
                }
                if (loadedBombs.Count > 0)
                {
                    WeaponSystemShipBomb bomb = (WeaponSystemShipBomb)loadedBombs.First(x => x.assignedSlotName == current.SlotName);
                    if (bomb != null)
                    {
                        Payload[current] = bomb;
                    }
                }
                if (assignedSystemsToModify.Count > 0)
                {
                    KeyValuePair<WeaponSystem, bool> entry = assignedSystemsToModify.First(x => x.Key.assignedSlotName == current.SlotName);
                    TryModifyWeaponSystem(current, entry.Key, entry.Value);
                }
            }
        }
        
        public bool TryModifyWeaponSystem(ShipWeaponSlot slot, Thing system, bool AddForInstalling = true)
        {
            if (AddForInstalling)
            {
                if (weaponsToInstall.ContainsKey(slot))
                {
                    weaponsToInstall.Remove(slot);
                }
                weaponsToInstall.Add(slot, system);
                return true;
            }
            else
            {
                if (weaponsToUninstall.ContainsKey(slot))
                {
                    weaponsToUninstall.Remove(slot);
                }
                weaponsToUninstall.Add(slot, system);
                return true;
            }
        }
       
        
        public override void Tick()
        {
            base.Tick();
            if (Find.Targeter.IsTargeting || Find.WorldTargeter.IsTargeting)
            {
                if (isTargeting)
                {
                    GhostDrawer.DrawGhostThing(UI.MouseCell(), Rotation, def, null, new Color(0.5f, 1f, 0.6f, 0.4f), AltitudeLayer.Blueprint);
                }
            }
            else
            {
                isTargeting = false;
            }

            foreach (var pawn in Passengers)
            {
                float num = 0.6f;
                num = 0.7f * num + 0.3f * num * StatDefOf.BedRestEffectiveness.defaultBaseValue;
                pawn.needs.rest.TickResting(num);
            }
            
            if (shipState == ShipState.Incoming)
            {
                drawTickOffset--;
                if (drawTickOffset <= 0)
                {
                    drawTickOffset = 0;
                }
                refuelableComp.ConsumeFuel(refuelableComp.Props.fuelConsumptionRate / 60f);
            }
            
            if (ReadyForTakeoff && ActivatedLaunchSequence)
            {
                timeToLiftoff--;
                if (ShouldWait)
                {
                    int num = GenDate.TicksPerHour;
                    timeToLiftoff += num;
                    timeWaited += num;
                    if (timeWaited >= maxTimeToWait)
                    {
                        ShouldWait = false;
                        timeToLiftoff = 0;
                    }
                }
                if (timeToLiftoff == 0)
                {
                    shipState = ShipState.Outgoing;
                    ActivatedLaunchSequence = false;
                    timeWaited = 0;
                }
            }

            if (shipState == ShipState.Outgoing )
            {
                drawTickOffset++;
                refuelableComp.ConsumeFuel(refuelableComp.Props.fuelConsumptionRate / 60f);
                if (Spawned)
                {                    
                    ShipBase_Traveling travelingShip = new ShipBase_Traveling(this);
                    GenSpawn.Spawn(travelingShip, Position, Map);
                    DeSpawn();
                }
            }
        }
        public void TryLaunch(RimWorld.Planet.GlobalTargetInfo target, PawnsArrivalModeDef arriveMode, TravelingShipArrivalAction arrivalAction, bool launchedAsSingleShip = false)
        {
            timeToLiftoff = 0;
            if (parentLandedShip == null)
            {
                shipState = ShipState.Outgoing;
                ShipBase_Traveling travelingShip = new ShipBase_Traveling(this, target, arriveMode, arrivalAction);

                var position = Position;
                var map = Map;
                DeSpawn();
                GenSpawn.Spawn(travelingShip, position, map);

                // TODO call method on the other ship
                if (LaunchAsFleet)
                {
                    foreach (ShipBase current in DropShipUtility.currentShipTracker.ShipsInFleet(fleetID))
                    {
                        if (current != this)
                        {
                            current.shipState = ShipState.Outgoing;
                            ShipBase_Traveling travelingShip2 = new ShipBase_Traveling(current, target, arriveMode, arrivalAction);
                            position = current.Position;
                            map = current.Map;
                            current.DeSpawn();
                            GenSpawn.Spawn(travelingShip2, position, map);
                        }
                    }
                }
            }
            else
            {
          //      Find.WorldSelector.Select(parentLandedShip);
                TravelingShipsUtility.LaunchLandedFleet(parentLandedShip, target.Tile, target.Cell, arriveMode, arrivalAction);
                landedShipCached = null;
                //Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.World, false);
            }
        }


        public override Vector3 DrawPos
        {
            get
            {
                return DropShipUtility.DrawPosAt(this, drawTickOffset);
            }
        }

        public override void Draw()
        {
            base.Draw();
            DropShipUtility.DrawDropSpotShadow(this, drawTickOffset);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Log.Warning("destroying " + ThingID);
            if (mode == DestroyMode.KillFinalize)
            {
                UnloadAndInjurePassengers();
                ShipUnload(true);
            }
            if (mode == DestroyMode.Deconstruct)
            {
                UnloadPassengers();
                ShipUnload(false);
            }
            if (mode == DestroyMode.Vanish)
            {
            }
            foreach (Building_ShipTurret current in assignedTurrets)
            {
                current.Destroy(mode);
            }

            DropShipUtility.currentShipTracker.RemoveShip(this);
            base.Destroy(mode);
        }

        public override void DeSpawn(DestroyMode destroyMode = DestroyMode.Vanish)
        {
            compShip.TryRemoveLord(Map);
            
            DeepsaveTurrets = true;
            //        SavePotentialWorldPawns();
            List<ShipWeaponSlot> slotsToRemove = new List<ShipWeaponSlot>();
            foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> current in installedTurrets)
            {
                if (current.Value != null)
                {
                    if (!current.Value.Destroyed)
                    {
                        current.Value.DeSpawn();
                    }
                    else
                    {
                        slotsToRemove.Add(current.Key);
                    }
                }
            }
            for (int i=0; i < slotsToRemove.Count; i++)
            {
                installedTurrets[slotsToRemove[i]] = null;
            }
            base.DeSpawn(destroyMode);
        }

        public void UnloadPassengers()
        {
            foreach (var pawn in Passengers.ToList())
            {
                PassengerModule?.Unload(pawn);
            }
            SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(Position, Map));
        }

        public void UnloadAndInjurePassengers()
        {
            foreach (var pawn in Passengers.ToList())
            {
                // TODO apply injuries instead of chance to kill
                PassengerModule?.Unload(pawn);
                if(Rand.Range(0, 1f) < 0.3f)
                {
                    pawn.Kill(new DamageInfo(DamageDefOf.Crush, 100));
                }
            }
        }

        public void ShipUnload(bool wasDestroyed = false)
        {
            if (GetDirectlyHeldThings() != null)
            {
                foreach (var thing in GetDirectlyHeldThings())
                {
                    Thing outThing;
                    GetDirectlyHeldThings()
                        .TryDrop(thing, PositionHeld, MapHeld, ThingPlaceMode.Near, out outThing);
                    if (wasDestroyed && Rand.Range(0, 1f) < 0.3f)
                    {
                        outThing.Destroy(DestroyMode.KillFinalize);
                    }
                }
            }
        }

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!Accepts(thing))
            {
                return false;
            }
            if (thing is Pawn pawn)
            {
                return PassengerModule?.Load(pawn) ?? false;
            }
            bool flag;
            if (thing.holdingOwner != null)
            {
                
                flag = thing.holdingOwner.TryTransferToContainer(thing, innerContainer);
                
            }
            else
            {
                flag = innerContainer.TryAdd(thing.SplitOff(thing.stackCount), true);
            }
            if (flag)
            {                
                return true;
            }
            return false;
        }
                
        public void PrepareForLaunchIn(int ticksToLiftoff, bool noOneLeftBehind = false)
        {
            ActivatedLaunchSequence = true;
            timeToLiftoff = ticksToLiftoff;
            NoneLeftBehind = noOneLeftBehind;
        }

        public virtual bool Accepts(Thing thing)
        {
            return innerContainer.CanAcceptAnyOf(thing);
        }

        public Map GetMap()
        {
            return Map;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //shouldDeepSave = true;
            DeepsaveTurrets = false;
            if (shouldSpawnTurrets)
            {
                foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> current in installedTurrets)
                {
                    if (current.Value != null && !current.Value.Spawned)
                    {
                        IntVec3 drawLoc = Position + DropShipUtility.AdjustedIntVecForShip(this, current.Key.turretPosOffset);
                        GenSpawn.Spawn(current.Value, drawLoc, Map);
                    }
                }
            }
            shouldSpawnTurrets = false;
            if (shipState == ShipState.Incoming)
            {
                SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
            }

            if (ShouldSpawnFueled)
            {
                Thing initialFuel = ThingMaker.MakeThing(ShipNamespaceDefOfs.Chemfuel);
                initialFuel.stackCount = 800;
                refuelableComp.Refuel(new List<Thing>(new Thing[] { initialFuel }));
                ShouldSpawnFueled = false;
            }
            DropShipUtility.InitializeDropShipSpawn(this);
            FirstSpawned = false;
        }

        public IntVec3 GetPosition()
        {
            return Position;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var menuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return menuOption;
            }

                Action action = delegate
                {
                    if (selPawn.CanReach(this, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        Job job = new Job(ShipNamespaceDefOfs.EnterShip, this);
                        selPawn.jobs.TryTakeOrderedJob(job);
                    }
                };
            if (PassengerModule?.HasEmptySeats() ?? false)
            {
                yield return new FloatMenuOption("EnterShip".Translate(), action, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else
            {
                yield return new FloatMenuOption("ShipPassengersFull".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
        }


        public bool TryInstallTurret(CompShipWeapon comp)
        {   
            if (comp.SProps.TurretToInstall != null)
            {
                ShipWeaponSlot slot = comp.slotToInstall;
                Building_ShipTurret turret = (Building_ShipTurret)ThingMaker.MakeThing(comp.SProps.TurretToInstall, null);
                turret.installedByWeaponSystem = comp.parent.def;
                installedTurrets[slot] = turret;
                turret.AssignParentShip(this);
                turret.assignedSlotName = slot.SlotName;
                turret.SetFactionDirect(Faction);
                if (slot.turretMinSize.x != turret.def.size.x)
                {
         //           turret.def.size.x = slot.turretMinSize.x;
                }
                if (slot.turretMinSize.z != turret.def.size.z)
                {
        //            turret.def.size.z = slot.turretMinSize.z;
                }
                IntVec3 drawLoc = Position + DropShipUtility.AdjustedIntVecForShip(this, slot.turretPosOffset);
                if (!turret.Spawned)
                {
                    GenSpawn.Spawn(turret, drawLoc, Map);
                }
                assignedTurrets.Add(turret);
                return true;
            }
            return false;
        }
        public bool TryInstallPayload( WeaponSystemShipBomb bomb,  CompShipWeapon comp)
        {
            if (comp.SProps.PayloadToInstall != null)
            {
                ShipWeaponSlot slot = comp.slotToInstall;
                loadedBombs.Add(bomb);
                Payload[slot] = bomb;
                return true;
            }
            return false;
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach(var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (Faction == Faction.OfPlayer)
            {

                if (ReadyForTakeoff)
                {
                    Command_Action command_Action = new Command_Action();
                    command_Action.defaultLabel = "CommandLaunchShip".Translate();
                    command_Action.defaultDesc = "CommandLaunchShipDesc".Translate();
                    command_Action.icon = DropShipUtility.LaunchSingleCommandTex;
                    command_Action.action = delegate
                    {
                        SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                        LaunchAsFleet = false;
                        StartChoosingDestination(this, LaunchAsFleet);
                    };
                    yield return command_Action;

                    if (fleetID != -1)
                    {

                        Command_Action command_Action3 = new Command_Action();
                        command_Action3.defaultLabel = "CommandLaunchFleet".Translate();
                        command_Action3.defaultDesc = "CommandLaunchFleetDesc".Translate();
                        command_Action3.icon = DropShipUtility.LaunchFleetCommandTex;
                        command_Action3.action = delegate
                        {
                            SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                            LaunchAsFleet = true;
                            StartChoosingDestination(this, LaunchAsFleet);
                        };
                        if (DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(x => !x.ReadyForTakeoff))
                        {
                            command_Action3.Disable("CommandLaunchFleetFailDueToNotReady".Translate());
                        }

                        yield return command_Action3;
                    }
                }
                {
                    Command_Action command_Action2 = new Command_Action();
                    command_Action2.defaultLabel = "CommandLoadShipCargo".Translate();
                    command_Action2.defaultDesc = "CommandLoadShipCargoDesc".Translate();
                    command_Action2.icon = DropShipUtility.LoadCommandTex;
                    command_Action2.action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_LoadShipCargo(Map, this));
                    };
                    yield return command_Action2;
                }
                {
                    Command_Action command_Action3 = new Command_Action();
                    command_Action3.defaultLabel = "CommandSetParkingPosition".Translate();
                    command_Action3.defaultDesc = "CommandSetParkingPositionDesc".Translate();
                    command_Action3.icon = DropShipUtility.ParkingSingle;
                    command_Action3.action = delegate
                    {
                        ParkingMap = Map;
                        ParkingPosition = Position;

                    };
                    yield return command_Action3;
                }
                if (ParkingMap != null && ReadyForTakeoff && Map != ParkingMap && ParkingPosition != Position)
                {
                    LaunchAsFleet = true;
                    Command_Action command_Action4 = new Command_Action();
                    command_Action4.defaultLabel = "CommandTravelParkingPosition".Translate();
                    command_Action4.defaultDesc = "CommandTravelParkingPositionDesc".Translate();
                    command_Action4.icon = DropShipUtility.ReturnParkingSingle;
                    command_Action4.action = delegate
                    {
                        TryLaunch(new GlobalTargetInfo(ParkingPosition, ParkingMap), PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.EnterMapFriendly, false);
                    };
                    yield return command_Action4;
                }

                if (ParkingMap != null && !DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(x => x.ParkingMap != null || !x.ReadyForTakeoff))
                {
                    Command_Action command_Action5 = new Command_Action();
                    command_Action5.defaultLabel = "CommandTravelParkingPositionFleet".Translate();
                    command_Action5.defaultDesc = "CommandTravelParkingPositionFleetDesc".Translate();
                    command_Action5.icon = DropShipUtility.ReturnParkingFleet;
                    command_Action5.action = delegate
                    {
                        foreach (ShipBase ship in DropShipUtility.currentShipTracker.ShipsInFleet(fleetID))
                        {
                            ship.TryLaunch(new GlobalTargetInfo(ship.ParkingPosition, ship.ParkingMap), PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.EnterMapFriendly, false);
                        }

                    };
                    yield return command_Action5;
                }
            }

        }

        public void StartChoosingDestination(ShipBase ship, bool launchAsFleet)
        {
            LaunchAsFleet = launchAsFleet;
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this));
            Find.WorldSelector.ClearSelection();
            int tile;
            if (parentLandedShip != null)
            {
                tile = parentLandedShip.Tile;
            }
            else
            {
                tile = Map.Tile;
            }
            
            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(ChoseWorldTarget), true, DropShipUtility.TargeterShipAttachment, true, delegate
            {
                DrawFleetLaunchRadii(launchAsFleet, tile);
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num <= MaxLaunchDistanceEverPossible(LaunchAsFleet))
                {
                    return null;
                }
                if (num > MaxLaunchDistanceEverPossible(LaunchAsFleet))
                {
                    return "TransportPodDestinationBeyondMaximumRange".Translate();
                }
                return "TransportPodNotEnoughFuel".Translate();
            });           

        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (parentLandedShip != null)
            {
                parentLandedShip.isTargeting = true;
            }
            isTargeting = true;
            int tile;
            if (parentLandedShip != null)
            {
                tile = parentLandedShip.Tile;
            }
            else
            {
                tile = Map.Tile;
            }
            bool canBomb = true;
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (LaunchAsFleet)
            {
                List<int> distances = new List<int>();
                for (int i=0; i< DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Count; i++)
                {
                    ShipBase ship = DropShipUtility.currentShipTracker.ShipsInFleet(fleetID)[i];
                    if (ship.compShip.CargoLoadingActive)
                    {
                        Messages.Message("MessageFleetLaunchImpossible".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    int num = (Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile));
                    if (num > ship.MaxLaunchDistanceEverPossible(true))
                    {
                        Messages.Message("MessageFleetLaunchImpossible".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    if (!(2*num > ship.MaxLaunchDistanceEverPossible(true)))
                    {
                        canBomb = false;
                    }
                }
            }
            else
            {
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);

                if (num > MaxLaunchDistanceEverPossible(LaunchAsFleet))
                {
                    Messages.Message("MessageTransportPodsDestinationIsTooFar"
                        .Translate(CompLaunchable.FuelNeededToLaunchAtDist(num).ToString("0.#")), MessageTypeDefOf.RejectInput);
                    return false;
                }
                if (!(2 * num > MaxLaunchDistanceEverPossible(true)))
                {
                    canBomb = false;
                }
            }
            
            MapParent mapParent = target.WorldObject as MapParent;
            if (mapParent != null && mapParent.HasMap)
            {
                Map myMap = Map;
                Map map = mapParent.Map;
                Current.Game.CurrentMap = map;
                Targeter targeter = Find.Targeter;
                Action actionWhenFinished = delegate
                {
                    if (Find.Maps.Contains(myMap))
                    {
                        Current.Game.CurrentMap = myMap;
                    }
                };
                targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate (LocalTargetInfo x)
                {
                    if (!ReadyForTakeoff || LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(s => !s.ReadyForTakeoff))
                    {
                        return;
                    }
                    // TODO was undecided drop
                    TryLaunch(x.ToGlobalTargetInfo(map), PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.EnterMapFriendly);
                }, null, actionWhenFinished, DropShipUtility.TargeterShipAttachment);
                return true;
            }
            
            if (target.WorldObject is Settlement || target.WorldObject is Site )
            {
                Find.WorldTargeter.closeWorldTabWhenFinished = false;
                MapParent localMapParent = target.WorldObject as MapParent;
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (!target.WorldObject.Faction.HostileTo(Faction.OfPlayer))
                {
                    list.Add(new FloatMenuOption("VisitSettlement".Translate(target.WorldObject.Label), delegate
                    {
                        if (!ReadyForTakeoff || LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(s => !s.ReadyForTakeoff))
                        {
                            return;
                        }
                        // TODO was undecided drop
                        TryLaunch(target, PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.StayOnWorldMap);
                        CameraJumper.TryHideWorld();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                list.Add(new FloatMenuOption("DropAtEdge".Translate(), delegate
                {
                    if (!ReadyForTakeoff || LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(s => !s.ReadyForTakeoff))
                    {
                        return;
                    }
                    TryLaunch(target, PawnsArrivalModeDefOf.EdgeDrop, TravelingShipArrivalAction.EnterMapFriendly);
                    CameraJumper.TryHideWorld();
                }, MenuOptionPriority.Default, null, null, 0f, null, null));
                //list.Add(new FloatMenuOption("DropInCenter".Translate(), delegate
                //{
                //    if (!ReadyForTakeoff || LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(s => !s.ReadyForTakeoff))
                //    {
                //        return;
                //    }
                //    TryLaunch(target, PawnsArriveMode.CenterDrop, TravelingShipArrivalAction.EnterMapFriendly);
                //    CameraJumper.TryHideWorld();
                //}, MenuOptionPriority.Default, null, null, 0f, null, null));

                    list.Add(new FloatMenuOption("AttackFactionBaseAerial".Translate(), delegate
                    {
                        if (!ReadyForTakeoff || LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(s => !s.ReadyForTakeoff))
                        {
                            return;
                        }
                        TryLaunch(target, PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.EnterMapAssault);
                        CameraJumper.TryHideWorld();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));


                    if (canBomb && (DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(x => x.installedTurrets.Any(y => y.Key.slotType == WeaponSystemType.Bombing && y.Value != null))) || loadedBombs.Any())
                    {
                        list.Add(new FloatMenuOption("BombFactionBase".Translate(), delegate
                        {
                            if (!ReadyForTakeoff || LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(fleetID).Any(s => !s.ReadyForTakeoff))
                            {
                                return;
                            }
                            performBombingRun = true;
                            TryLaunch(target, PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.BombingRun);
                            CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                

                Find.WindowStack.Add(new FloatMenu(list));
                return true;
            }
            if (Find.World.Impassable(target.Tile))
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            
            // TODO was undecided drop
            TryLaunch(target, PawnsArrivalModeDefOf.CenterDrop, TravelingShipArrivalAction.StayOnWorldMap);
            return true;
        }

        private void DrawFleetLaunchRadii(bool launchAsFleet, int tile)
        {
            GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistanceEverPossible(launchAsFleet));
            GenDraw.DrawWorldRadiusRing(tile, (int)(MaxLaunchDistanceEverPossible(launchAsFleet) * 0.48f));
            if (launchAsFleet)
            {
                foreach (ShipBase ship in DropShipUtility.currentShipTracker.ShipsInFleet(fleetID))
                {
                    GenDraw.DrawWorldRadiusRing(tile, ship.MaxLaunchDistanceEverPossible(launchAsFleet));
                    GenDraw.DrawWorldRadiusRing(tile, (int)(ship.MaxLaunchDistanceEverPossible(launchAsFleet)*0.48f));
                }
            }
        }               

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref FirstSpawned, "FirstSpawned", false, false);
            Scribe_Values.Look<bool>(ref ActivatedLaunchSequence, "ActivatedLaunchSequence", false, false);
            Scribe_Values.Look<bool>(ref ShouldWait, "ShouldWait", false, false);
            Scribe_Values.Look<bool>(ref NoneLeftBehind, "NoneLeftBehind", false, false);
            Scribe_Values.Look<bool>(ref keepShipReference, "keepShipReference", false, false);
            Scribe_Values.Look<bool>(ref shouldSpawnTurrets, "shouldSpawnTurrets", false, false);
            //Scribe_Values.Look<bool>(ref shouldDeepSave, "shouldDeepSave", true, false);
            Scribe_Values.Look<string>(ref ShipNick, "ShipNick", "Ship", false);
            Scribe_Values.Look<ShipState>(ref shipState, "shipState", ShipState.Stationary, false);
            Scribe_Values.Look<int>(ref timeToLiftoff, "timeToLiftoff", 200, false);
            Scribe_Values.Look<int>(ref drawTickOffset, "drawTickOffset", 0, false);
            Scribe_Values.Look<int>(ref timeWaited, "timeWaited", 200, false);

            
            Scribe_References.Look(ref ParkingMap, "ParkingMap");
            Scribe_Values.Look<IntVec3>(ref ParkingPosition, "ParkingPosition", IntVec3.Zero , false);



            Scribe_Values.Look<bool>(ref DeepsaveTurrets, "DeepsaveTurrets", false, false);
            assignedTurrets.RemoveAll(x => x == null);
            if (DeepsaveTurrets)
            {
                Scribe_Collections.Look<Building_ShipTurret>(ref assignedTurrets, "assignedTurrets", LookMode.Deep, new object[0]);
            }
            else
            {
                Scribe_Collections.Look<Building_ShipTurret>(ref assignedTurrets, "assignedTurrets", LookMode.Reference, new object[0]);
            }

    
            Scribe_Collections.Look<WeaponSystemShipBomb>(ref loadedBombs, "loadedBombs", LookMode.Reference, new object[0]);
            if (assignedSystemsToModify.Count > 0)
            {
                Scribe_Collections.Look<WeaponSystem, bool>(ref assignedSystemsToModify, "assignedSystemsToModify", LookMode.Reference, LookMode.Value);
            }
            
            Scribe_Deep.Look<ThingOwner>(ref innerContainer, "innerContainer", new object[]
            {
             this
            });
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                InitiateInstalledTurrets();
            }
        }
    }
}
