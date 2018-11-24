using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobGiver_LeaveInShip : ThinkNode_JobGiver
    {       
        protected override Job TryGiveJob(Pawn pawn)
        {
            foreach (var ship in DropShipUtility.CurrentFactionShips(pawn).InRandomOrder())
            {
                if (ship.Map.reservationManager.CanReserve(pawn, ship, ship.TryGetComp<CompShip>().sProps.maxPassengers))
                {
                    return new Job(ShipNamespaceDefOfs.LeaveInShip, pawn, ship);
                }
            }
            return null;
        }
    }
}
