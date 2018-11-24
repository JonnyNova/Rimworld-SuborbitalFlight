using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class WeaponSystem : ThingWithComps
    {
        protected StunHandler stunner;
        
        public bool isInstalled = false;

        protected Vector3 drawPosOffset;

        public string assignedSlotName;

        public ShipWeaponSlot slotToInstall;

        public WeaponSystemType weaponSystemType;

        protected LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        public WeaponSystem()
        {
            stunner = new StunHandler(this);
        }

        public override void Tick()
        {
            if (!Spawned)
            {
                base.Tick();
                stunner.StunHandlerTick();
            }
            if (isInstalled && Spawned)
            {
                DeSpawn();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            isInstalled = false;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<StunHandler>(ref stunner, "stunner", new object[]
            {
                this
            });

            Scribe_Values.Look<bool>(ref isInstalled, "isInstalled", false, false);
            Scribe_Values.Look<string>(ref assignedSlotName, "assignedSlotName");
            Scribe_Values.Look<Vector3>(ref drawPosOffset, "drawPosOffset");
            Scribe_Values.Look<WeaponSystemType>(ref weaponSystemType, "weaponSystemType");
            Scribe_TargetInfo.Look(ref forcedTarget, "forcedTarget");
            if (slotToInstall != null)
            {
                Scribe_References.Look<ShipWeaponSlot>(ref slotToInstall, "slotToInstall");
            }
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (absorbed)
            {
                return;
            }
            stunner.Notify_DamageApplied(dinfo, true);
            absorbed = false;
        }
        
        public bool ThreatDisabled()
        {
            CompPowerTrader comp = base.GetComp<CompPowerTrader>();
            if (comp == null || !comp.PowerOn)
            {
                    CompRefuelable comp3 = base.GetComp<CompRefuelable>();
                if (comp3 != null || !comp3.HasFuel)
                {
                    CompMannable comp2 = base.GetComp<CompMannable>();
                    if (comp2 == null || !comp2.MannedNow)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
