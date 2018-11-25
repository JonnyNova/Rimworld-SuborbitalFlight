using System;
using System.Collections.Generic;
using System.Diagnostics;
using FrontierDevelopments.SuborbitalFlight.Module;
using Verse.AI;

namespace OHUShips
{
    public class JobDriver_EnterShip : JobDriver
    {
        private ShipBase Ship => (ShipBase) TargetThingA;
        private CompPassengerModule PassengerModule => Ship?.PassengerModule;
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return PassengerModule?.HasEmptySeats() ?? false;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //yield return Toils_Reserve.Reserve(TargetIndex.A, 10, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 50;
            toil.WithProgressBarToilDelay(TargetIndex.A);
            yield return toil;
            yield return new Toil
            {
                initAction = delegate
                {
                    ShipBase ship = Ship;
                    Action action = delegate
                    {
                        // TODO carried things go into storage
                        if (PassengerModule?.Load(pawn) ?? false)
                        {
                            pawn.ClearMind();
                        }
                        
//                        if (pawn.carryTracker.CarriedThing != null)
//                        {
//                            ship.TryAcceptThing(pawn.carryTracker.CarriedThing);
//                        }
                    };

                    action();                    
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}

