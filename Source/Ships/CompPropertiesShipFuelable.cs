using RimWorld;

namespace OHUShips
{
    public class CompPropertiesShipFuelable : CompProperties_Refuelable
    {
        public CompPropertiesShipFuelable()
        {
            this.compClass = typeof (CompShipFuelable);
        }
    }
}
