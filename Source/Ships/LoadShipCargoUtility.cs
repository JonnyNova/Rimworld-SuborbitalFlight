using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    public class LoadShipCargoUtility
    {
        public static Job JobLoadShipCargo(Pawn p, ShipBase ship)
        {
            if (p.jobs.jobQueue.Any(x => x.job.def == ShipNamespaceDefOfs.LoadContainerMultiplePawns))
            {
                    return null;
                
            }
                Thing thing = LoadShipCargoUtility.FindThingToLoad(p, ship);
                // TODO desperate?
                TransferableOneWay transferable = TransferableUtility.TransferableMatchingDesperate(thing, ship.compShip.LeftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
                if (thing != null && transferable != null)
                {
                    int thingCount = transferable.CountToTransfer;
                    if (thingCount < 0)
                    {
                        thingCount = 1;
                    }
                    return new Job(ShipNamespaceDefOfs.LoadContainerMultiplePawns, thing, ship)
                    {
                        count = thingCount,
                        ignoreForbidden = true,
                        playerForced = true

                    };
                }
            
            else
            {
                Log.Message("No Transferable found.");
            }
            return null;
        }

        private static Thing FindThingToLoad(Pawn p, ShipBase ship)
        {
            var neededThings = new HashSet<Thing>();
            foreach (var transferableOneWay in ship.compShip.LeftToLoad)
            {
                if (transferableOneWay.CountToTransfer > 0)
                {
                    for (int j = 0; j < transferableOneWay.things.Count; j++)
                    {
                        neededThings.Add(transferableOneWay.things[j]);
                    }
                }
            }
            if (!neededThings.Any())
            {
                return null;
            }
            //Predicate<Thing> validator = (Thing x) => LoadShipCargoUtility.neededThings.Contains(x) && p.CanReserve(x, 1) && !p.Map.reservationManager.IsReservedByAnyoneOf(x, p.Faction);
            //Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null);
            Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, (Thing x) => neededThings.Contains(x) && p.CanReserve(x, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);

            if (thing == null)
            {
                foreach (Thing current in neededThings)
                {
                    Pawn pawn = current as Pawn;
                    if (pawn != null && (!pawn.IsColonist || pawn.Downed) && p.CanReserveAndReach(pawn, PathEndMode.Touch, Danger.Deadly, 10))
                    {
                        return pawn;
                    }
                }
                return null;
            }
            //Log.Message("Returning Thing: " + thing.ToString());
            return thing;
        }

        public static bool HasJobOnShip(Pawn pawn, ShipBase ship)
        {
            return ship.compShip.AnythingLeftToLoad && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.CanReserveAndReach(ship, PathEndMode.Touch, pawn.NormalMaxDanger(), 10, 1, ReservationLayerDefOf.Floor, true) && LoadShipCargoUtility.FindThingToLoad(pawn, ship) != null;
        }

        public static Lord FindLoadLord(ShipBase ship, Map map)
        {
            if (map == null) return null;
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                LordJob_LoadShipCargo lordJob_LoadAndEnterTransporters = lords[i].LordJob as LordJob_LoadShipCargo;
                //if (lordJob_LoadAndEnterTransporters != null) Log.Message("Found Lordjob");
                if (lordJob_LoadAndEnterTransporters != null && lordJob_LoadAndEnterTransporters.ship == ship)
                {
                    //Log.Message("Found for REmoval");
                    return lords[i];
                }
            }
            return null;
        }

        
    }
}
