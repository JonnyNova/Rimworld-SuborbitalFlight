using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace OHUShips
{
    public class Dialog_LoadShipCargo : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private const float TitleRectHeight = 40f;

        private const float BottomAreaHeight = 55f;

        private Map map;

        private ShipBase ship;

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Dialog_LoadShipCargo.Tab tab;

        private float lastMassFlashTime = -9999f;

        private bool massUsageDirty = true;

        private float cachedMassUsage;

        private bool daysWorthOfFoodDirty = true;

        private Pair<float, float> cachedDaysWorthOfFood;

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private static List<TabRecord> tabsList = new List<TabRecord>();

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)UI.screenHeight);
            }
        }

        protected override float Margin
        {
            get
            {
                return 0f;
            }
        }

        private float MassCapacity
        {
            get
            {
                return ship.compShip.sProps.maxCargo;
            }
        }

        private float PassengerCapacity
        {
            get
            {
                return ship.compShip.sProps.maxPassengers;
            }
        }

        private string TransportersLabelFull
        {
            get
            {
                return ship.LabelCap + " : " + ship.ShipNick;
            }
        }

        private string TransportersLabelShort
        {
            get
            {
                return ship.ShipNick.CapitalizeFirst();
            }
        }

        private float MassUsage
        {
            get
            {
                if (massUsageDirty)
                {
                    massUsageDirty = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.DontIgnore, true, false);
                    cachedMassUsage += MassAlreadyStored();
                }
                return cachedMassUsage;
            }
        }

        public float MassAlreadyStored()
        {
            float num = 0f;
            for (int i=0; i < ship.GetDirectlyHeldThings().Count; i++)
            {
                num += ship.GetDirectlyHeldThings()[i].stackCount * ship.GetDirectlyHeldThings()[i].GetStatValue(StatDefOf.Mass);
            }
            return num;
        }

        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (daysWorthOfFoodDirty)
                {
                    daysWorthOfFoodDirty = false;
                    float first = DropShipUtility.ApproxDaysWorthOfFood_Ship(ship, transferables);
                    cachedDaysWorthOfFood = new Pair<float, float>(first, DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload));
                }
                return cachedDaysWorthOfFood;
            }
        }
       
        public Dialog_LoadShipCargo(Map map, ShipBase ship)
        {
            this.map = map;
            this.ship = ship;
            forcePause = true;
            absorbInputAroundWindow = true;
            OHUShipsModSettings.CargoLoadingActive = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }

        private bool EnvironmentAllowsEatingVirtualPlantsNow
        {
            get
            {
                return VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(map.Tile);
            }
        }

        public static void RemoveExistingTransferableItems(
            TransferableOneWay transferable,
            ShipBase ship)
        {
            RemoveExistingTransferableItems(transferable, new List<ShipBase>(new[] {ship}));
        }

        public static void RemoveExistingTransferableItems(
            TransferableOneWay transferable,
            IEnumerable<ShipBase> ships)
        {
            foreach (var currentShip in ships)
            {
                foreach (var thing in transferable.things)
                {
                    if (currentShip.GetDirectlyHeldThings().Contains(thing))
                        transferable.things.Remove(thing);
                }
            }
        }

        public static void RemoveExistingTransferableItems(TransferableOneWay transferable, Map map)
        {
            RemoveExistingTransferableItems(transferable, DropShipUtility.ShipsOnMap(map));
        }

        private int PawnsToTransfer
        {
            get
            {
                return transferables.Where(x => x.AnyThing is Pawn && x.CountToTransfer > 0).Count();
            }
        }

        private string PassengerUse
        {
            get
            {
                return PawnsToTransfer + "/" + PassengerCapacity + " " +"ShipPassengers".Translate();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 40f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "LoadTransporters".Translate(TransportersLabelFull));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Dialog_LoadShipCargo.tabsList.Clear();
            Dialog_LoadShipCargo.tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
            {
                this.tab = Dialog_LoadShipCargo.Tab.Pawns;
            }, this.tab == Dialog_LoadShipCargo.Tab.Pawns));
            Dialog_LoadShipCargo.tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
            {
                this.tab = Dialog_LoadShipCargo.Tab.Items;
            }, this.tab == Dialog_LoadShipCargo.Tab.Items));
            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, Dialog_LoadShipCargo.tabsList);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            Rect rect3 = rect2;
            rect3.xMin += rect2.width - pawnsTransfer.TotalNumbersColumnsWidths;
            rect3.y += 32f;
            
//            TransferableUIUtility.DrawMassInfo(rect3, MassUsage, MassCapacity, "TransportersMassUsageTooltip".Translate(), lastMassFlashTime, true);
//            CaravanUIUtility.DrawDaysWorthOfFoodInfo(new Rect(rect3.x, rect3.y + 22f, rect3.width, rect3.height), DaysWorthOfFood.First, DaysWorthOfFood.Second, EnvironmentAllowsEatingVirtualPlantsNow, true, 3.40282347E+38f);
            DrawPassengerCapacity(rect3);

            DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax += 59f;
            bool flag = false;
            Dialog_LoadShipCargo.Tab tab = this.tab;
            if (tab != Dialog_LoadShipCargo.Tab.Pawns)
            {
                if (tab == Dialog_LoadShipCargo.Tab.Items)
                {
                    itemsTransfer.OnGUI(inRect2, out flag);
                }
            }
            else
            {
                pawnsTransfer.OnGUI(inRect2, out flag);
            }
            if (flag)
            {
                CountToTransferChanged();
            }
            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        private void AddToTransferables(Thing t, int countAlreadyIn = 0)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                transferables.Add(transferableOneWay);
            }

            // Dialog_LoadShipCargo.RemoveExistingTransferable(transferableOneWay, null, ship);

            transferableOneWay.things.Add(t);
            transferableOneWay.AdjustBy(-countAlreadyIn);

        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect0 = new Rect(0f, rect.height - 55f, 300f, 30f);
            
            Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true) && TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(false);
            }
            Rect rect3 = new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                Close(true);
            }
            if (Prefs.DevMode)
            {
                float num = 200f;
                float num2 = BottomButtonSize.y / 2f;
                Rect rect5 = new Rect(rect.width - num, rect.height - 55f, num, num2);
                if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false, true) && DebugTryLoadInstantly())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    Close(false);
                }
                Rect rect6 = new Rect(rect.width - num, rect.height - 55f + num2, num, num2);
                if (Widgets.ButtonText(rect6, "Dev: Select everything", true, false, true))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    SetToLoadEverything();
                }
            }
        }

        private void DrawPassengerCapacity(Rect rect3)
        {
            GUI.color = PawnsToTransfer > PassengerCapacity ? Color.red : Color.gray;
            Vector3 vector = Text.CalcSize(PassengerUse);
            Rect rect2 = new Rect(rect3.xMax - vector.x, rect3.y + 44f, vector.x, vector.y);
            Widgets.Label(rect2, PassengerUse);
            GUI.color = Color.white;
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            AddPawnsToTransferables();
            AddItemsToTransferables();
        //    RemoveExistingTransferables();
            pawnsTransfer = new TransferableOneWayWidget(null, Faction.OfPlayer.Name, TransportersLabelShort, "FormCaravanColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, () => MassCapacity - MassUsage, 24f, false, -1, true);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);

            itemsTransfer = new TransferableOneWayWidget(from x in transferables
                                                              where x.ThingDef.category != ThingCategory.Pawn
                                                              select x, Faction.OfPlayer.Name, TransportersLabelShort, "FormCaravanColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, () => MassCapacity - MassUsage, 24f, false, -1, true);
            CountToTransferChanged();            
        }

        private bool DebugTryLoadInstantly()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableUtility.Transfer(transferables[i].things, transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    ship.GetDirectlyHeldThings().TryAdd(splitPiece, true);
                });
            }
            return true;
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
            if (!CheckForErrors(pawnsFromTransferables))
            {
                return false;
            }
            if (!AssignTransferablesToShip())
            {
                return false;
            }

            var pawns = (from x in pawnsFromTransferables
                where x.IsColonist && !x.Downed
                select x).ToList();
            foreach (Pawn current in pawns)
            {
                if (current.Spawned)
                {
                    current.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            }
            Lord newLord = LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadShipCargo(ship), map, pawns);
            ship.compShip.cargoLoadingActive = true;
            Messages.Message("MessageShipCargoLoadStarted".Translate(ship.ShipNick), ship, MessageTypeDefOf.NeutralEvent);
            return true;
        }

        private bool AssignTransferablesToShip()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                RemoveExistingTransferableItems(transferables[i], ship);
                if (transferables[i].CountToTransfer > 0)
                {
                    ship.compShip.AddToTheToLoadList(transferables[i], transferables[i].CountToTransfer);
 //                   TransferableUIUtility.ClearEditBuffer(transferables[i]);
                }             
            }
            return true;
        }        

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (!transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
            {
                Messages.Message("CantSendEmptyTransportPods".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (MassUsage > MassCapacity)
            {
                FlashMass();
                Messages.Message("TooBigShipMassUsage".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (PawnsToTransfer > PassengerCapacity)
            {
                Messages.Message("ShipSeatsFull".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            Pawn pawn = pawns.Find((Pawn x) => !x.MapHeld.reachability.CanReach(x.PositionHeld, ship, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)));
            if (pawn != null)
            {
                Messages.Message("PawnCantReachTransporters".Translate(pawn.LabelShort).CapitalizeFirst(), MessageTypeDefOf.RejectInput);
                return false;
            }
            Map map = ship.Map;
            for (int i = 0; i < transferables.Count; i++)
            {
                if (transferables[i].ThingDef.category == ThingCategory.Item)
                {
                    int CountToTransfer = transferables[i].CountToTransfer;
                    int num = 0;
                    if (CountToTransfer > 0)
                    {
                        for (int j = 0; j < transferables[i].things.Count; j++)
                        {
                            Thing thing = transferables[i].things[j];
                            if (map.reachability.CanReach(thing.Position, ship, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                            {
                                num += thing.stackCount;
                                if (num >= CountToTransfer)
                                {
                                    break;
                                }
                            }
                        }
                        if (num < CountToTransfer)
                        {
                            if (CountToTransfer == 1)
                            {
                                Messages.Message("TransporterItemIsUnreachableSingle"
                                    .Translate(transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput);
                            }
                            else
                            {
                                Messages.Message("TransporterItemIsUnreachableMulti"
                                    .Translate(CountToTransfer, transferables[i].ThingDef.label),
                                    MessageTypeDefOf.RejectInput);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void AddPawnsToTransferables()
        {
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(map, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                AddToTransferables(list[i]);
            }
        }

        private bool isPlayerBase
        {
            get
            {
                var mapParent = Find.WorldObjects.SettlementAt(ship.Tile);
                if (mapParent != null)
                {
                    if (mapParent.Faction != Faction.OfPlayer)
                    return true;
                }
                return false;
            }
        }

        private void AddItemsToTransferables()
        {
           // List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(map, false, false);

            List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(map, false, isPlayerBase);
            for (int i = 0; i < list.Count; i++)
            {
                int alreadyIn = 0;
                Thing thingAlreadyIn = ship.GetDirectlyHeldThings().FirstOrDefault(x => x == list[i]);
                if (thingAlreadyIn != null)
                {
                    alreadyIn = thingAlreadyIn.stackCount;
                }
                AddToTransferables(list[i], alreadyIn);
            }
        }

        private void FlashMass()
        {
            lastMassFlashTime = Time.time;
        }

        private void SetToLoadEverything()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableUtility.Transfer(transferables[i].things, transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    ship.GetDirectlyHeldThings().TryAdd(splitPiece, true);
                });
            }
            CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            massUsageDirty = true;
            daysWorthOfFoodDirty = true;
        }

        public override void Close(bool doCloseSound = true)
        {
            OHUShipsModSettings.CargoLoadingActive = false;
            base.Close(doCloseSound);
        }
    }
}
