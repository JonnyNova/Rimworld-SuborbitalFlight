using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobDriver_LoadCargoMultiple : JobDriver_HaulToContainer
    {
        private ShipBase ship
        {
            get
            {
               return (ShipBase)TargetB;
            }
        }

        private TransferableOneWay transferable
        {
            get
            {
                // TODO should this be desperate?
                return TransferableUtility.TransferableMatchingDesperate(TargetA.Thing, ship.compShip.LeftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            }
        }


        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            //toil.AddFailCondition(() => ShipFull(ship));
            toil.tickAction += delegate
            {
                if (ShipFull(ship))
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return toil;
            yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            Toil toilPickup = Toils_Haul.StartCarryThing(TargetIndex.A, false, true);//.FailOn(() => ShipFull(ship));
            yield return toilPickup;
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(toil, TargetIndex.A);
            Toil toil2 = Toils_Haul.CarryHauledThingToContainer();

            toil2.tickAction += delegate
            {
                if (ShipFull(ship, false))
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return toil2;
            Toil toil3 = Toils_Haul.DepositHauledThingInContainer(TargetIndex.B, TargetIndex.None);
            //toil3.AddFailCondition(() => ShipFull(ship));
            yield return toil3;
            yield break;
        }
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            ////Log.Message("Reserving 1");
            //pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job, 1, -1, null);
            ////Log.Message("Reserving 2");
            //pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job, 10, 1, null);
            ////Log.Message("Reserving 3");
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null);// && pawn.Reserve(job.GetTarget(TargetIndex.B), job, 20, 1, null);
        }


        private Action RestoreRemainingThings(Thing t, int amount)
        {
            return delegate
            {
                pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToStorageJob(pawn, t));
            };
        }

        private bool ShipFull(ShipBase ship, bool firstCheck = true)
        {
            //Log.Message("Checking");
            CompShip compShip = ship.compShip;
                if (transferable != null)
                {
                    if (firstCheck && job.count > transferable.CountToTransfer)
                    {
                        return true;
                    }
                    if (!firstCheck && TargetA.Thing.stackCount > transferable.CountToTransfer)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            
            
        }

    }
}
