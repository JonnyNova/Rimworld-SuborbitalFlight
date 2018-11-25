using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace OHUShips
{
    public class CompShip : ThingComp
    {
        private readonly List<TransferableOneWay> leftToLoad = new List<TransferableOneWay>();

        public bool CargoLoadingActive => !leftToLoad.NullOrEmpty();
        
        public ShipBase ship
        {
            get
            {
                return (ShipBase)parent;
            }
        }

        public CompProperties_Ship sProps
        {
            get
            {
                return props as CompProperties_Ship;
            }
        }

        public Graphic dropShadow
        {
            get
            {
                return  GraphicDatabase.Get<Graphic_Single>(sProps.ShadowGraphicPath, ShaderDatabase.Transparent, Vector2.one, Color.white);
            }
        }

        public Texture2D fleetIconTexture
        {
            get
            {
                return ContentFinder<Texture2D>.Get(sProps.FleetIconGraphicPath, true);
            }
        }

        public Thing FirstThingLeftToLoad
        {
            get
            {
                if (leftToLoad.NullOrEmpty())
                {
                    return null;
                }
                TransferableOneWay transferableOneWay = leftToLoad.Find((TransferableOneWay x) => x.CountToTransfer > 0 && x.HasAnyThing);
                if (transferableOneWay != null)
                {
                    return transferableOneWay.AnyThing;
                }
                return null;
            }
        }

        public bool AnythingLeftToLoad
        {
            get
            {
                return FirstThingLeftToLoad != null;
            }
        }

        public List<TransferableOneWay> LeftToLoad => leftToLoad;

        public bool LoadingOnlyPawnsRemain()
        {
            return leftToLoad
                       .Where(transferable => transferable.HasAnyThing)
                       .Select(transferable => transferable.AnyThing)
                       .Where(thing => thing != null)
                       .FirstOrDefault(thing => thing.GetType() == typeof(Pawn)) == null;
        }

        public bool CancelLoadCargo(Map map)
        {
            if (!CargoLoadingActive)
            {
                return false;
            }
            leftToLoad.Clear();
            TryRemoveLord(map);
            return true;
        }

        public void AddToTheToLoadList(TransferableOneWay t, int count)
        {
            if (!t.HasAnyThing || t.CountToTransfer <= 0)
            {
                //Log.Message("NoThingsToTransfer");
                return;
            }
            if (TransferableUtility.TransferableMatching<TransferableOneWay>(t.AnyThing, leftToLoad, TransferAsOneMode.Normal) != null)
            {
                Log.Error("Transferable already exists.");
                return;
            }

            TransferableOneWay transferableOneWay = new TransferableOneWay();
            leftToLoad.Add(transferableOneWay);
            transferableOneWay.things.AddRange(t.things);
            transferableOneWay.AdjustTo(count);
        }


        public void TryRemoveLord(Map map)
        {
            List<Pawn> pawns = new List<Pawn>();
            Lord lord = LoadShipCargoUtility.FindLoadLord(ship, map);
            if (lord != null)
            {
                foreach (Pawn p in pawns)
                {
                    lord.Notify_PawnLost(p, PawnLostCondition.LeftVoluntarily);
                }
            }
        }

        public void Notify_PawnEntered(Pawn p)
        {
            p.ClearMind(true);
            SubtractFromToLoadList(p, 1);
        }

        public void SubtractFromToLoadList(Thing t, int count)
        {
            //Log.Message("Remaining transferables: " + leftToLoad.Count.ToString() + " with Pawns:" + leftToLoad.FindAll(x => x.AnyThing is Pawn).Count.ToString());
            if (leftToLoad == null)
            {
                return;
            }
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(t, leftToLoad, TransferAsOneMode.Normal);
            if (transferableOneWay == null)
            {
                return;
            }
            transferableOneWay.AdjustBy(-count);
            if (transferableOneWay.CountToTransfer <= 0)
            {
                leftToLoad.Remove(transferableOneWay);
            }

            if (!AnythingLeftToLoad)
            {
                TryRemoveLord(parent.Map);
                leftToLoad.Clear();
              
                Messages.Message("MessageFinishedLoadingShipCargo".Translate(ship.ShipNick), parent, MessageTypeDefOf.TaskCompletion);
            }
        }

        public void NotifyItemAdded(Thing t, int count = 0)
        {
            SubtractFromToLoadList(t, count);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look<ShipWeaponSlot>(ref sProps.weaponSlots, "weaponSlots", LookMode.Deep);
        }
    }
}
