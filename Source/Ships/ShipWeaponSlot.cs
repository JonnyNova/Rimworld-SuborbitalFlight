using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace OHUShips
{
    public class ShipWeaponSlot : ILoadReferenceable, IExposable
    {
        public string SlotName;
        public WeaponSystemType slotType;
        private int loadID = -1;

        private int LoadID
        {
            get
            {
                if (loadID == -1)
                {
                    loadID = DropShipUtility.currentShipTracker.GetNextWeaponSlotID();
                }
                return loadID;
            }                
        }

        public IntVec3 turretPosOffset;

        public IntVec2 turretMinSize = new IntVec2(1, 1);

        public AltitudeLayer altitudeLayer = AltitudeLayer.ItemImportant;
               
        public string GetUniqueLoadID()
        {
            return "ShipWeaponSlot_" + LoadID;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look<string>(ref SlotName, "SlotName", "");
            Scribe_Values.Look<WeaponSystemType>(ref slotType, "slotType", WeaponSystemType.LightCaliber);
            Scribe_Values.Look<IntVec2>(ref turretMinSize, "turretMinSize", IntVec2.One);
            Scribe_Values.Look<IntVec3>(ref turretPosOffset, "posOffset", IntVec3.Zero);
            Scribe_Values.Look<int>(ref loadID, "loadID");
            Scribe_Values.Look<WeaponSystemType>(ref slotType, "slotType");
        }
    }
}
