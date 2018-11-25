using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FrontierDevelopments.SuborbitalFlight.Module
{
    public class CompPropertiesPassengerModule : CompProperties
    {
        public int seatCount = 1;
        
        public CompPropertiesPassengerModule()
        {
            compClass = typeof(CompPassengerModule);
        }
    }
    
    public class CompPassengerModule : ThingComp, IThingHolder
    {
        private ThingOwner<Pawn> passengers;
        
        private CompPropertiesPassengerModule Props => (CompPropertiesPassengerModule) props;

        public IEnumerable<Pawn> Passengers => passengers;

        public CompPassengerModule()
        {
            passengers = new ThingOwner<Pawn>(this);
        }

        public int Capacity => Props.seatCount;

        public int OpenSeats => Props.seatCount - passengers.Count;
        
        public bool HasPilot => 
            Passengers
                .FirstOrDefault(pawn =>
                    pawn.Faction == parent.Faction
                    && pawn.RaceProps.intelligence == Intelligence.Humanlike
                    && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                    && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) != null;
        
        public bool HasEmptySeats(int count = 1)
        {
            return passengers.Count + count <= Props.seatCount;
        }

        public bool Load(Pawn pawn)
        {
            if (HasEmptySeats())
            {
                pawn.DeSpawn();
                if (pawn.holdingOwner != null
                    ? pawn.holdingOwner.TryTransferToContainer(pawn, passengers)
                    : passengers.TryAdd(pawn)) 
                    return true;
                GenSpawn.Spawn(pawn, pawn.PositionHeld, pawn.MapHeld);
                Log.Warning("unable to load pawn " + pawn.ThingID + " into " + parent.ThingID);
            }

            return false;
        }

        public bool Unload(Pawn pawn)
        {
            Thing thing;
            return passengers.TryDrop(pawn, ThingPlaceMode.Near, out thing);
        }

        public override void PostExposeData()
        {
            Scribe_Deep.Look(ref passengers, "passengers", this);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return passengers;
        }
    }
}