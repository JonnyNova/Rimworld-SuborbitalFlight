﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    public class ITab_ShipCargo : ITab
    {

        private enum Tab
        {
            Passengers,
            Cargo,
            Weapons
        }
        
        private const float TopPadding = 20f;
        
        private const float ThingIconSize = 28f;

        private ITab_ShipCargo.Tab tab;

        private const float ThingRowHeight = 28f;

        private const float ThingLeftX = 36f;

        private const float StandardLineHeight = 22f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;
                
        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static List<Thing> workingInvList = new List<Thing>();

        public ShipBase ship
        {
            get
            {
                return (ShipBase)SelThing;
            }
        }

        private bool CanControl
        {
            get
            {
                return ship.Faction == Faction.OfPlayer;
            }
        }

        public ITab_ShipCargo()
        {
            size = new Vector2(600f, 500f);
            labelKey = "TabShipCargo";
        }
        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y);
            GUI.BeginGroup(rect);
            Rect rect2 = new Rect(rect.x, rect.y + 20f, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect2, ship.ShipNick);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect rect3 = rect2;
            rect3.y = rect2.yMax + 100;
            rect3.height = rect.height - rect2.height ;

            Widgets.DrawMenuSection(rect3);
            List<TabRecord> list = new List<TabRecord>();

            list.Add(new TabRecord("ShipPassengers".Translate(), delegate
            {
                tab = ITab_ShipCargo.Tab.Passengers;
            }, tab == ITab_ShipCargo.Tab.Passengers));
            
            list.Add(new TabRecord("ShipCargo".Translate(), delegate
            {
                tab = ITab_ShipCargo.Tab.Cargo;
            }, tab == ITab_ShipCargo.Tab.Cargo));

            list.Add(new TabRecord("ShipWeapons".Translate(), delegate
            {
                tab = ITab_ShipCargo.Tab.Weapons;
            }, tab == ITab_ShipCargo.Tab.Weapons));
            TabDrawer.DrawTabs(rect3, list);
            rect3 = rect3.ContractedBy(9f);
        //    GUI.BeginGroup(rect3);

            GUI.color = Color.white;

            if (tab == Tab.Passengers)
            {
                DrawPassengers(rect3);
            }
            else if (tab == Tab.Cargo)
            {
                DrawCargo(rect3, true);
            }
            else if (tab == Tab.Weapons)
            {
                DrawWeaponSlots(rect3);
            }
          
      //      GUI.EndGroup();
            GUI.EndGroup();
        }

        private void DrawCargo(Rect inRect, bool nonPawn)
        {
            Text.Font = GameFont.Small;
            Rect rect = inRect.ContractedBy(4f);
            GUI.BeginGroup(rect);
            GUI.color = Color.white;
            Rect totalRect = new Rect(0f, 0f, rect.width-50f, 300f);
            Rect viewRect = new Rect(0f, 0f, rect.width, scrollViewHeight);
            Widgets.BeginScrollView(totalRect, ref scrollPosition, viewRect);
            float num = 0f;
            if (ship.GetDirectlyHeldThings() != null)
            {
                Text.Font = GameFont.Small;
                for (int i = 0; i < ship.GetDirectlyHeldThings().Count; i++)
                {
                    Thing thing = ship.GetDirectlyHeldThings()[i];
                    Pawn pawn = thing as Pawn;
                    if (nonPawn)
                    {
                        if (pawn == null || (pawn != null && !pawn.def.race.Humanlike))
                        {
                            DrawThingRow(ref num, viewRect.width-100f, thing);
                        }
                    }
                    else
                    {
                        if (pawn != null && pawn.def.race.Humanlike)
                        {
                            DrawThingRow(ref num, viewRect.width-100f, thing);
                        }
                    }
                }
            }
            scrollViewHeight = num + 30f;            
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawPassengers(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Rect rect = inRect.ContractedBy(4f);
            GUI.BeginGroup(rect);
            GUI.color = Color.white;
            Rect totalRect = new Rect(0f, 0f, rect.width-50f, 300f);
            Rect viewRect = new Rect(0f, 0f, rect.width, scrollViewHeight);
            Widgets.BeginScrollView(totalRect, ref scrollPosition, viewRect);
            float num = 0f;
            Text.Font = GameFont.Small;
            foreach (var pawn in ship.Passengers.ToList())
            {
                DrawThingRow(ref num, viewRect.width-100f, pawn);
            }
            scrollViewHeight = num + 30f;            
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        public void DrawWeaponSlots(Rect inRect)
        {
            Rect rect1 = inRect;
            float num = inRect.y;
            foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> currentWeapon in ship.installedTurrets)
            {
                DrawWeaponsTurretRow(ref num, rect1.width, currentWeapon);
            }
            foreach (KeyValuePair<ShipWeaponSlot, WeaponSystemShipBomb> currentbomb in ship.Payload)
            {
                DrawWeaponsPayloadRow(ref num, rect1.width, currentbomb);
            }
        }

        private void DrawWeaponsTurretRow(ref float y, float width, KeyValuePair<ShipWeaponSlot, Building_ShipTurret> currentWeapon)
        {
            Rect rectslotName = new Rect(10f, y, 100f, 30f);
            Widgets.Label(rectslotName, currentWeapon.Key.SlotName);

            Rect rectslotIcon = new Rect(rectslotName.xMax + 5f, y, 30f, 30f);
            if (currentWeapon.Value == null)
            {
                Widgets.DrawWindowBackground(rectslotIcon);
            }
            else
            {
                Widgets.DrawWindowBackground(rectslotIcon);
                Texture2D tex = ContentFinder<Texture2D>.Get(currentWeapon.Value.def.building.turretTopGraphicPath);
                GUI.DrawTexture(rectslotIcon, tex);
            }

            if (Mouse.IsOver(rectslotIcon))
            {
                GUI.color = ITab_ShipCargo.HighlightColor;
                GUI.DrawTexture(rectslotIcon, TexUI.HighlightTex);
            }
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(rectslotIcon))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (currentWeapon.Value == null)
                {
                    List<Thing> list = DropShipUtility.availableWeaponsForSlot(ship.Map, currentWeapon.Key);
                    list.OrderBy(x => x.Position.DistanceToSquared(ship.Position));
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing weapon = list[i];
                        Action action = new Action(delegate
                        {
                            ship.TryModifyWeaponSystem(currentWeapon.Key, weapon, true);
                        });

                        FloatMenuOption newOption = new FloatMenuOption("Install".Translate() + weapon.Label, action);
                        opts.Add(newOption);
                    }
                }
                else
                {
                    Action action = new Action(delegate
                    {
                        ship.TryModifyWeaponSystem(currentWeapon.Key, currentWeapon.Value, false);
                    });
                    FloatMenuOption newOption = new FloatMenuOption("Uninstall".Translate() + currentWeapon.Value.Label, action);
                    opts.Add(newOption);
                }
                if (opts.Count < 1)
                {
                    opts.Add(new FloatMenuOption("None", null));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            Rect rect3 = new Rect(rectslotIcon.xMax + 10f, y, width - rectslotName.width - rectslotIcon.width - 10f, 30f);

            if (currentWeapon.Value == null && !ship.weaponsToInstall.Any(x => x.Key == currentWeapon.Key))
            {
                Widgets.Label(rect3, "NoneInstalled".Translate());
            }
            else
            {
                ShipWeaponSlot installingSlot = ship.weaponsToInstall.FirstOrDefault(x => x.Key == currentWeapon.Key).Key;
                if (installingSlot != null)
                {
                    Widgets.Label(rect3, "InstallingShipWeapon".Translate(ship.weaponsToInstall[installingSlot].LabelCap));
                }
                else
                {
                    Widgets.Label(rect3, currentWeapon.Value.def.LabelCap);
                }

            }
            y += 35f;
        }

        private void DrawWeaponsPayloadRow(ref float y, float width, KeyValuePair<ShipWeaponSlot, WeaponSystemShipBomb> currentWeapon)
        {
            Rect rectslotName = new Rect(10f, y, 100f, 30f);
            Widgets.Label(rectslotName, currentWeapon.Key.SlotName);

            Rect rectslotIcon = new Rect(rectslotName.xMax + 5f, y, 30f, 30f);
            if (currentWeapon.Value == null)
            {
                Widgets.DrawWindowBackground(rectslotIcon);
            }
            else
            {
                Widgets.DrawWindowBackground(rectslotIcon);
                Texture2D tex = currentWeapon.Value.def.uiIcon;
                GUI.DrawTexture(rectslotIcon, currentWeapon.Value.def.uiIcon);
            }

            if (Mouse.IsOver(rectslotIcon))
            {
                GUI.color = ITab_ShipCargo.HighlightColor;
                GUI.DrawTexture(rectslotIcon, TexUI.HighlightTex);
            }
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(rectslotIcon))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (currentWeapon.Value == null)
                {
                    List<Thing> list = DropShipUtility.availableWeaponsForSlot(ship.Map, currentWeapon.Key);
                    list.OrderBy(x => x.Position.DistanceToSquared(ship.Position));
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing weapon = list[i];
                        Action action = new Action(delegate
                        {
                            ship.TryModifyWeaponSystem(currentWeapon.Key, weapon, true);
                        });

                        FloatMenuOption newOption = new FloatMenuOption("Install".Translate() + weapon.Label, action);
                        opts.Add(newOption);
                    }
                }
                else
                {
                    Action action = new Action(delegate
                    {
                        ship.TryModifyWeaponSystem(currentWeapon.Key, currentWeapon.Value, false);
                    });
                    FloatMenuOption newOption = new FloatMenuOption("Uninstall".Translate() + currentWeapon.Value.Label, action);
                    opts.Add(newOption);
                }
                if (opts.Count < 1)
                {
                    opts.Add(new FloatMenuOption("None", null));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            Rect rect3 = new Rect(rectslotIcon.xMax + 10f, y, width - rectslotName.width - rectslotIcon.width - 10f, 30f);

            if (currentWeapon.Value == null)
            {
                Widgets.Label(rect3, "NoneInstalled".Translate());
            }
            else if (ship.weaponsToInstall.Any(x => x.Key == currentWeapon.Key))
            {
                Widgets.Label(rect3, "InstallingShipWeapon".Translate(currentWeapon.Value.LabelCap));
            }
            else
            {
                Widgets.Label(rect3, currentWeapon.Value.def.LabelCap);
            }

            y += 35f;
        }

        private void DrawThingRow(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;
            if (CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, DropShipUtility.DropTexture))
                {
                    Verse.Sound.SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High);
                    InterfaceDrop(thing, ship);
                }
                rect.width -= 24f;
            }
            if (Mouse.IsOver(rect))
            {
                GUI.color = ITab_ShipCargo.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);        
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ITab_ShipCargo.ThingLabelColor;
            Rect rect3 = new Rect(36f, y, width - 36f, 28f);
            string text = thing.LabelCap;

            Widgets.Label(rect3, text);
            y += 32f;
        }

        private void InterfaceDrop(Thing thing, ShipBase ship)
        {
            switch (thing)
            {
                case Pawn pawn:
                    ship?.PassengerModule?.Unload(pawn);
                    // TODO probs not needed
                    Lord LoadLord = LoadShipCargoUtility.FindLoadLord(ship, ship.Map);
                    if (LoadLord != null)
                    {
                        LoadLord.ownedPawns.Remove(pawn);
                    }
                    break;
                default:
                    ship.GetDirectlyHeldThings().TryDrop(thing, ThingPlaceMode.Near, out thing);
                    break;
            }
        }
    }
}
