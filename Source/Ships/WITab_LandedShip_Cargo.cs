using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace OHUShips
{
    public class WITab_LandedShip_Cargo : WITab
    {
        private const float MassCarriedLineHeight = 22f;

        private Vector2 scrollPosition;

        private float scrollViewHeight;

        private List<Thing> items = new List<Thing>();

        public WITab_LandedShip_Cargo()
        {
            labelKey = "ShipCargo";
        }

        public LandedShip landedShip
        {
            get
            {
                return SelObject as LandedShip;
            }
        }

        private List<TransferableImmutable> getTransferableImmutables()
        {
            return new List<TransferableImmutable>(new[]
            {
                new TransferableImmutable
                {
                    things = items
                }
            });
        }
        
        protected override void FillTab()
        {
            float num = 0f;
            DrawMassUsage(ref num);
            GUI.BeginGroup(new Rect(0f, num, size.x, size.y - num));
            UpdateItemsList();
            CaravanItemsTabUtility.DoRows(size, getTransferableImmutables(), base.SelCaravan, ref scrollPosition, ref scrollViewHeight);
            items.Clear();
            GUI.EndGroup();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            UpdateItemsList();
            size = CaravanItemsTabUtility.GetSize(getTransferableImmutables(), PaneTopY, true);
            items.Clear();
        }

        private void DrawMassUsage(ref float curY)
        {
            curY += 10f;
            Rect rect = new Rect(10f, curY, size.x - 10f, 100f);
            float massUsage = base.SelCaravan.MassUsage;
            float massCapacity = landedShip.allLandedShipMassCapacity;
            if (massUsage > massCapacity)
            {
                GUI.color = Color.red;
            }
            Text.Font = GameFont.Small;
            Widgets.Label(rect, "MassCarried".Translate(
                massUsage.ToString("0.##"),
                massCapacity.ToString("0.##")));
            GUI.color = Color.white;
            curY += 22f;
        }

        private void UpdateItemsList()
        {
            items.Clear();
            items.AddRange(landedShip.AllLandedShipCargo);
        }
    }
}
