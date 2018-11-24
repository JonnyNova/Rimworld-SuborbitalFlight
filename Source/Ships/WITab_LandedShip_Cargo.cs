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
            this.labelKey = "ShipCargo";
        }

        public LandedShip landedShip
        {
            get
            {
                return this.SelObject as LandedShip;
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
            this.DrawMassUsage(ref num);
            GUI.BeginGroup(new Rect(0f, num, this.size.x, this.size.y - num));
            this.UpdateItemsList();
            CaravanItemsTabUtility.DoRows(this.size, getTransferableImmutables(), base.SelCaravan, ref this.scrollPosition, ref this.scrollViewHeight);
            this.items.Clear();
            GUI.EndGroup();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            this.UpdateItemsList();
            this.size = CaravanItemsTabUtility.GetSize(getTransferableImmutables(), this.PaneTopY, true);
            this.items.Clear();
        }

        private void DrawMassUsage(ref float curY)
        {
            curY += 10f;
            Rect rect = new Rect(10f, curY, this.size.x - 10f, 100f);
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
            this.items.Clear();
            this.items.AddRange(landedShip.AllLandedShipCargo);
        }
    }
}
