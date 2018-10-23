using RimWorld;
using Verse;

namespace OHUShips
{
    public class CompShipFuelable : CompRefuelable
    {
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            // do nothing to stop it from placing fuel on the ground
        }
    }
}