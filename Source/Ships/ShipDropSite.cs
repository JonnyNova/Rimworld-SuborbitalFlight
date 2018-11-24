using RimWorld.Planet;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse.Sound;

namespace OHUShips
{
    public class ShipDropSite : MapParent
    {
        private const int timeToRemove = 2000;

        private bool forcedRemoval = false;
        
        private int timePresent = 0;

        private Material cachedMat;

        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                { 
                    cachedMat = MaterialPool.MatFrom("World/WorldObjects/AircraftDropSpot", ShaderDatabase.WorldOverlayTransparentLit, base.Faction.Color, WorldMaterials.WorldObjectRenderQueue);
                }
                return cachedMat;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref timePresent, "timePresent", 0);
            Scribe_Values.Look<bool>(ref forcedRemoval, "forcedRemoval", false);
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if ((!base.Map.mapPawns.AnyPawnBlockingMapRemoval && timePresent > timeToRemove && !Map.listerThings.AllThings.Any(x => x.Faction == Faction.OfPlayer || x is ShipBase_Traveling)) || forcedRemoval)
            {
                alsoRemoveWorldObject = true;
                return true;
            }

            alsoRemoveWorldObject = false ;
            return false;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (!Map.listerThings.AllThings.Any(x => x is ShipBase || x is ShipBase_Traveling))
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandRemoveDropsite".Translate();
                command_Action.defaultDesc = "CommandRemoveDropsiteDesc".Translate();
                command_Action.icon = DropShipUtility.CancelTex;
                command_Action.action = delegate
                {
                    SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                    forcedRemoval = true;
                };
                yield return command_Action;
            }
        }

        public override void Tick()
        {
            base.Tick();
            timePresent ++;
        }
    }
}
