using System.Linq;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobGiver_LeaveInShip : ThinkNode_JobGiver
    {       
        protected override Job TryGiveJob(Pawn pawn)
        {
            foreach (var ship in DropShipUtility.CurrentFactionShips(pawn).Where(ship => ship.PassengerModule?.HasEmptySeats() ?? false).InRandomOrder())
            {
                if (ship.Map.reservationManager.CanReserve(pawn, ship, ship.PassengerModule?.Capacity ?? 0))
                {
                    return new Job(ShipNamespaceDefOfs.LeaveInShip, pawn, ship);
                }
            }
            return null;
        }
    }
}
