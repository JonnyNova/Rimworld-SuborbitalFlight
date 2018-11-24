using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OHUShips
{
    public class ShipTracker : WorldObject
    {
        public static void GenerateTracker()
        {
            ShipTracker shipTracker = (ShipTracker)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipTracker);
            int tile = 0;
            while (!(Find.WorldObjects.AnyWorldObjectAt(tile) || Find.WorldGrid[tile].biome == BiomeDefOf.Ocean))
            {
                tile = Rand.Range(0, Find.WorldGrid.TilesCount);
            }
            shipTracker.Tile = tile;
            Find.WorldObjects.Add(shipTracker);
        }
        
        public override bool SelectableNow
        {
            get
            {
                return false;
            }
        }

        public override void Draw()
        {
        }

        private int nextFleetID = 0;

        private int nextWeaponSlotID = 0;

        public Dictionary<int, string> PlayerFleetManager = new Dictionary<int, string>();

        public Dictionary<string, List<ShipBase>> shipsInFlight = new Dictionary<string, List<ShipBase>>();

        public List<ShipBase> AllWorldShips = new List<ShipBase>();

        public List<LandedShip> AllLandedShips
        {
            get
            {
                List<LandedShip> tmp = new List<LandedShip>();
                for (int i = 0; i < Find.WorldObjects.AllWorldObjects.Count; i++)
                {
                    LandedShip ship = Find.WorldObjects.AllWorldObjects[i] as LandedShip;
                    if (ship != null)
                    {
                        tmp.Add(ship);
                    }
                }
                return tmp;
            }
        }
        
        public List<TravelingShips> AllTravelingShips
        {
            get
            {
                List<TravelingShips> tmp = new List<TravelingShips>();
                for (int i=0; i < Find.WorldObjects.AllWorldObjects.Count; i++)
                {
                    TravelingShips ship = Find.WorldObjects.AllWorldObjects[i] as TravelingShips;
                    if (ship != null)
                    {
                        tmp.Add(ship);
                    }
                }
                return tmp;
            }
        }

        public void RemoveShip(ShipBase ship)
        {
            AllWorldShips.Remove(ship);
            AllWorldShips.RemoveAll(x => x == null);
        }

        public List<ShipBase> PlayerShips
        {
            get
            {
                return AllWorldShips.FindAll(x => x.Faction == Faction.OfPlayer);
            }
        }
        public void AddNewFleetEntry()
        {
            PlayerFleetManager.Add(GetNextFleetId(), "TabFleetManagement".Translate() + " " + nextFleetID);
        }
        public void AddNewFleetEntry(string newName)
        {
            PlayerFleetManager.Add(GetNextFleetId(), newName);
        }
        public void DeleteFleetEntry(int ID)
        {
            PlayerFleetManager.Remove(ID);
        }
        
        public int GetNextFleetId()
        {
            return GetNextID(ref nextFleetID);
        }

        private int GetNextID(ref int nextID)
        {
            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Log.Warning("Getting next unique ID during saving or loading. This may cause bugs.");
            }
            int result = nextID;
            nextID++;
            if (nextID == 2147483647)
            {
                Log.Warning("Next ID is at max value. Resetting to 0. This may cause bugs.");
                nextID = 0;
            }
            return result;
        }

        public int GetNextWeaponSlotID()
        {
            return GetNextID(ref nextWeaponSlotID);
        }

        public List<ShipBase> ShipsInFleet(int ID)
        {
            return AllWorldShips.FindAll(x => x.fleetID == ID);
        }

        public bool PawnIsTravelingInShip(Pawn pawn)
        {
            for(int i = 0; i < AllTravelingShips.Count; i++)
            {
                TravelingShips cur = AllTravelingShips[i];
                if (cur.ContainsPawn(pawn))
                {
                    return true;
                }
            }

            return false;
        }
                
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref nextFleetID, "nextFleetID");
            Scribe_Values.Look<int>(ref nextWeaponSlotID, "nextWeaponSlotID");
            Scribe_Collections.Look<int, string>(ref PlayerFleetManager, "PlayerFleetManager", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look<ShipBase>(ref AllWorldShips, "AllWorldShips", LookMode.Reference, new object[0]);            
        }

        
    }
}
