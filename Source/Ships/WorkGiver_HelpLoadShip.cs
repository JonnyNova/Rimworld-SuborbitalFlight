using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class WorkGiver_HelpLoadShip : WorkGiver
    {
        public override Job NonScanJob(Pawn pawn)
        {
            foreach (var ship in ShipsNeedHelpLoading(pawn.Map).InRandomOrder())
            {
                var job = LoadShipCargoUtility.JobLoadShipCargo(pawn, ship);
                if (job != null) return job;
            }
            return null;
        }

        private IEnumerable<ShipBase> ShipsNeedHelpLoading(Map map)
        {
            return map.listerBuildings.allBuildingsColonist
                .Where(building => building.GetType() == typeof(ShipBase))
                .Cast<ShipBase>()
                .Where(ship => ship.GetComp<CompShip>().CargoLoadingActive);
        }
    }
}