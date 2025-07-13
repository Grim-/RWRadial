using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RWRadial
{
    [StaticConstructorOnStartup]
    public class RadialMenuWindow : Window
    {
        private List<RadialMenuItem> allMenuItems;
        private List<RadialMenuItem> currentPageItems;
        private Stack<List<RadialMenuItem>> menuStack;
        private Stack<int> pageStack;
        private Vector2 currentWindowSize;
        private Vector2 centerPosition => new Vector2(this.windowRect.size.x / 2, this.windowRect.size.y / 2 - Settings.heightOffset);
        private bool isFavoritesMenu;
        private Pawn sourcePawn;

        private AbilityRadialPagerSettings Settings => RWRadialMod.Settings;
        private GameComp_RadialFavouritesTracker FavoritesTracker => Current.Game.GetComponent<GameComp_RadialFavouritesTracker>();

        private int itemsPerPage => Settings.itemsPerPage;
        private int currentPage = 0;
        private int totalPages => Mathf.CeilToInt((float)allMenuItems.Count / itemsPerPage);

        private bool hasMultiplePages => totalPages > 1;

        private float SpacePerItem => Mathf.Lerp(Settings.maxSpacePerItem, Settings.minSpacePerItem,
            Mathf.InverseLerp(Settings.minPageCount, Settings.maxPageCount, currentPageItems.Count));

        private float baseRadius => Settings.baseRadius;

        private float radius => baseRadius + (currentPageItems.Count * SpacePerItem);

        private float itemSize => Mathf.Lerp(Settings.maxItemSize, Settings.minItemSize,
            Mathf.InverseLerp(Settings.minPageCount, Settings.maxPageCount, currentPageItems.Count));

        private int hoveredIndex = -1;

        protected static Texture2D BackgroundTex = ContentFinder<Texture2D>.Get("UI/RadialBG");

        public RadialMenuWindow(List<RadialMenuItem> menuItems, bool isFavoritesMenu = false)
        {
            this.allMenuItems = menuItems.OrderBy(x => x.order).ToList();
            this.isFavoritesMenu = isFavoritesMenu;
            this.sourcePawn = menuItems.FirstOrDefault()?.parentPawn;
            this.menuStack = new Stack<List<RadialMenuItem>>();
            this.pageStack = new Stack<int>();
            this.currentPage = 0;
            UpdateCurrentPageItems();
            this.currentWindowSize = CalculateWindowSize();
            this.doWindowBackground = false;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.forcePause = false;
            this.preventCameraMotion = false;
            this.layer = WindowLayer.Super;
            this.drawShadow = false;
        }

        public RadialMenuWindow(Pawn pawn, List<Command> abilityGizmos, bool isFavoritesMenu = false)
        {
            this.sourcePawn = pawn;
            this.allMenuItems = BuildAbilityMenuItems(pawn, abilityGizmos);
            this.isFavoritesMenu = isFavoritesMenu;
            this.menuStack = new Stack<List<RadialMenuItem>>();
            this.pageStack = new Stack<int>();
            this.currentPage = 0;
            UpdateCurrentPageItems();
            this.currentWindowSize = CalculateWindowSize();
            this.doWindowBackground = false;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.forcePause = false;
            this.preventCameraMotion = false;
            this.layer = WindowLayer.Super;
            this.drawShadow = false;
        }

        public override Vector2 InitialSize => currentWindowSize;

        private List<RadialMenuItem> BuildAbilityMenuItems(Pawn pawn, List<Command> abilityGizmos)
        {
            var categoryGroups = abilityGizmos
                .GroupBy(g => AbilityRadialPager.GetAbilityCategory(g))
                .OrderBy(group => group.Key)
                .ToList();

            List<RadialMenuItem> menuItems = new List<RadialMenuItem>();

            foreach (var categoryGroup in categoryGroups)
            {
                var categoryItem = new RadialMenuItem(
                    pawn,
                    categoryGroup.Key,
                    "",
                    categoryGroup.First().icon as Texture2D
                );

                foreach (var abilityGizmo in categoryGroup)
                {
                    RadialMenuItem abilityItem = new RadialMenuItem(
                        pawn,
                        GetGizmoLabel(abilityGizmo),
                        AbilityRadialPager.GetAbilityDescription(abilityGizmo),
                        abilityGizmo.icon as Texture2D,
                        () => ExecuteAbilityGizmo(abilityGizmo))
                    {
                        sourceGizmo = abilityGizmo,
                        defName = AbilityRadialPager.GetAbilityDefName(abilityGizmo)
                    };

                    categoryItem.subItems.Add(abilityItem);
                }

                if (categoryItem.subItems.Count == 1)
                {
                    var singleAbility = categoryItem.subItems.First();
                    singleAbility.label = categoryGroup.Key;
                    menuItems.Add(singleAbility);
                }
                else if (categoryItem.subItems.Any())
                {
                    menuItems.Add(categoryItem);
                }
            }

            if (Settings.ShowFavouritesMenu)
            {
                menuItems.Add(new RadialMenuItem(pawn, "Favourites", "Favourites", null, () => OpenFavoritesMenu(), 20));
            }

            return menuItems.OrderBy(x => x.order).ToList();
        }



        private void OpenFavoritesMenu()
        {
            if (sourcePawn == null) return;

            var favoriteDefNames = FavoritesTracker.PawnAbilityFavourites.ContainsKey(sourcePawn)
                ? FavoritesTracker.PawnAbilityFavourites[sourcePawn]
                : new List<string>();

            List<RadialMenuItem> favoriteItems = new List<RadialMenuItem>();

            foreach (var menuItem in GetAllAbilityItems(allMenuItems))
            {
                if (!string.IsNullOrEmpty(menuItem.defName) && favoriteDefNames.Contains(menuItem.defName))
                {
                    favoriteItems.Add(menuItem);
                }
            }

            if (favoriteItems.Any())
            {
                RadialMenuWindow favWindow = new RadialMenuWindow(favoriteItems, true);
                Find.WindowStack.Add(favWindow);
                favWindow.windowRect.x = UI.screenWidth / 2f - favWindow.currentWindowSize.x / 2f;
                favWindow.windowRect.y = (UI.screenHeight / 2f - favWindow.currentWindowSize.y / 2f) - Settings.heightOffset;
                Close();
            }
            else
            {
                Messages.Message("No favorite abilities found.", MessageTypeDefOf.RejectInput);
            }
        }

        private List<RadialMenuItem> GetAllAbilityItems(List<RadialMenuItem> items)
        {
            List<RadialMenuItem> result = new List<RadialMenuItem>();
            foreach (var item in items)
            {
                if (item.HasSubItems)
                {
                    result.AddRange(GetAllAbilityItems(item.subItems));
                }
                else if (item.sourceGizmo != null)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private string GetGizmoLabel(Command gizmo)
        {
            return gizmo.Label;
        }

        private void ExecuteAbilityGizmo(Command abilityGizmo)
        {
            if (!abilityGizmo.Disabled)
            {
                abilityGizmo.ProcessInput(Event.current);
            }
            else
            {
                Log.Message(abilityGizmo.disabledReason);
            }
        }

        private void UpdateCurrentPageItems()
        {
            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, allMenuItems.Count);
            currentPageItems = allMenuItems.GetRange(startIndex, endIndex - startIndex);
            currentPageItems = currentPageItems.OrderBy(x => x.order).ToList();
        }

        private float extraPageIndicatorHeight = 20f;

        private Vector2 CalculateWindowSize()
        {
            float menuRadius = radius;
            float labelHeight = Text.CalcHeight("Sample", 200f);
            float pageIndicatorHeight = hasMultiplePages ? extraPageIndicatorHeight : 0f;
            float totalRadius = menuRadius + itemSize / 2f + labelHeight + pageIndicatorHeight + 30f;
            float size = (totalRadius * 2f) + 10f;
            return new Vector2(size, size);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 mousePos = Event.current.mousePosition;
            hoveredIndex = GetHoveredItemIndex(mousePos);

            Vector2 newSize = CalculateWindowSize();
            if (newSize != currentWindowSize)
            {
                ResizeWindow(newSize);
            }

            if (BackgroundTex != null)
            {
                float bgSize = (radius + itemSize / 2f) * 2f;
                Rect bgRect = new Rect(
                    centerPosition.x - bgSize / 2f,
                    centerPosition.y - bgSize / 2f,
                    bgSize,
                    bgSize
                );
                Widgets.DrawTextureFitted(bgRect, BackgroundTex, 1);
            }

            DrawRadialMenu(inRect);
            HandleInput();
        }

        private void DrawRadialMenu(Rect rect)
        {
            for (int i = 0; i < currentPageItems.Count; i++)
            {
                DrawMenuItem(i, currentPageItems[i]);
            }

            if (menuStack.Count > 0)
            {
                Rect backButton = new Rect(centerPosition.x - Settings.backButtonSize / 2,
                    centerPosition.y - Settings.backButtonSize / 2, Settings.backButtonSize, Settings.backButtonSize);
                GUI.color = hoveredIndex == -2 ? Color.yellow : Color.white;

                if (hoveredIndex >= 0 && hoveredIndex < currentPageItems.Count && currentPageItems[hoveredIndex].icon != null)
                {
                    GUI.DrawTexture(backButton, currentPageItems[hoveredIndex].icon);
                }
                else
                {
                    GUI.DrawTexture(backButton, TexButton.CloseXBig);
                }
                GUI.color = Color.white;
            }

            if (hasMultiplePages)
            {
                DrawPageNavigation();
            }

            if (hoveredIndex >= 0 && hoveredIndex < currentPageItems.Count)
            {
                RadialMenuItem hoveredItem = currentPageItems[hoveredIndex];
                bool isEnabled = hoveredItem.sourceGizmo?.Disabled != true;
                string displayText = isEnabled ? hoveredItem.label : $"{hoveredItem.label} ({hoveredItem.sourceGizmo?.disabledReason ?? "Disabled"})";

                if (hoveredItem.IsFavoritable && sourcePawn != null)
                {
                    bool isFavorited = FavoritesTracker.IsFavourite(sourcePawn, hoveredItem.defName);
                    displayText += isFavorited ? " ★" : " (Right-click to favorite)";
                }

                Vector2 labelSize = Text.CalcSize(displayText);
                float yOffset = hasMultiplePages ? 40f : 20f;
                Rect hoveredItemLabel = new Rect(centerPosition.x - labelSize.x / 2f, centerPosition.y + yOffset, labelSize.x, labelSize.y);
                GUI.Label(hoveredItemLabel, displayText);
            }

            if (isFavoritesMenu)
            {
                string favText = "Favorites Menu";
                Vector2 favTextSize = Text.CalcSize(favText);
                Rect favTextRect = new Rect(centerPosition.x - favTextSize.x / 2f, centerPosition.y - radius - 30f, favTextSize.x, favTextSize.y);
                GUI.color = Color.yellow;
                GUI.Label(favTextRect, favText);
                GUI.color = Color.white;
            }
        }

        private void DrawPageNavigation()
        {
            string pageText = $"{currentPage + 1} / {totalPages}";
            Vector2 pageTextSize = Text.CalcSize(pageText);
            Rect pageTextRect = new Rect(centerPosition.x - pageTextSize.x / 2f, centerPosition.y + 20f, pageTextSize.x, pageTextSize.y);
            GUI.Label(pageTextRect, pageText);

            if (currentPage > 0)
            {
                Rect prevButton = new Rect(centerPosition.x - 60f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                GUI.color = hoveredIndex == -3 ? Color.yellow : Color.white;
                GUI.DrawTexture(prevButton, TexUI.ArrowTexLeft);
                GUI.color = Color.white;
            }

            if (currentPage < totalPages - 1)
            {
                Rect nextButton = new Rect(centerPosition.x + 40f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                GUI.color = hoveredIndex == -4 ? Color.yellow : Color.white;
                GUI.DrawTexture(nextButton, TexUI.ArrowTexRight);
                GUI.color = Color.white;
            }
        }

        private void ResizeWindow(Vector2 newSize)
        {
            currentWindowSize = newSize;
            Vector2 center = GetCenterScreenPosition();
            windowRect = new Rect(center.x, center.y, newSize.x, newSize.y);
        }

        private Vector2 GetCenterScreenPosition()
        {
            return new Vector2(UI.screenWidth / 2f - this.currentWindowSize.x / 2f,
                (UI.screenHeight / 2f - this.currentWindowSize.y / 2f) - Settings.heightOffset);
        }

        private void DrawMenuItem(int index, RadialMenuItem item)
        {
            float angle = (360f / currentPageItems.Count) * index - 90f;
            Vector2 itemPos = GetItemPosition(angle);

            float extraHoverSize = index == hoveredIndex ? Settings.hoverSizeIncrease : 1f;

            Rect itemRect = new Rect(itemPos.x - itemSize * extraHoverSize / 2f, itemPos.y - itemSize * extraHoverSize / 2f,
                itemSize * extraHoverSize, itemSize * extraHoverSize);

            bool isEnabled = item.sourceGizmo?.Disabled != true;
            Color itemColor = isEnabled ? (item.color != Color.white ? item.color : Color.white) : Color.gray;

            GUI.color = itemColor;

            if (index == hoveredIndex)
            {
                GUI.color = Color.yellow;
                string tooltip = isEnabled ? item.description : $"{item.description}\n\nDisabled: {item.sourceGizmo?.disabledReason ?? "Unknown reason"}";
                TooltipHandler.TipRegion(itemRect, tooltip);
            }

            if (item.icon != null)
            {
                GUI.DrawTexture(itemRect, item.icon);
            }
            else
            {
                GUI.DrawTexture(itemRect, TexButton.Infinity);
            }

            if (item.IsFavoritable && sourcePawn != null && FavoritesTracker.IsFavourite(sourcePawn, item.defName))
            {
                Rect starRect = new Rect(itemPos.x + itemSize * extraHoverSize / 2f - 12f, itemPos.y - itemSize * extraHoverSize / 2f, 12f, 12f);
                GUI.color = Color.yellow;
                GUI.Label(starRect, "★");
            }

            GUI.color = Color.white;

            if (Settings.showLabels)
            {
                Vector2 labelSize = Text.CalcSize(item.label);
                Rect labelRect = new Rect(itemPos.x - labelSize.x / 2f, itemPos.y + itemSize * extraHoverSize / 2f + 5f,
                                            labelSize.x, labelSize.y);
                GUI.Label(labelRect, item.label);
            }

            if (item.HasSubItems)
            {
                Rect arrowRect = new Rect(itemPos.x + itemSize * extraHoverSize / 2f - 8f, itemPos.y - itemSize * extraHoverSize / 2f, 8f, 8f);
                GUI.DrawTexture(arrowRect, BaseContent.WhiteTex);
            }
        }

        private Vector2 GetItemPosition(float angleDegrees)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float x = centerPosition.x + Mathf.Cos(angleRad) * radius;
            float y = centerPosition.y + Mathf.Sin(angleRad) * radius;
            return new Vector2(x, y);
        }

        private int GetHoveredItemIndex(Vector2 mousePos)
        {
            if (menuStack.Count > 0)
            {
                Rect backButton = new Rect(centerPosition.x - Settings.backButtonSize / 2,
                    centerPosition.y - Settings.backButtonSize / 2, Settings.backButtonSize, Settings.backButtonSize);
                if (backButton.Contains(mousePos))
                {
                    return -2;
                }
            }

            if (hasMultiplePages)
            {
                if (currentPage > 0)
                {
                    Rect prevButton = new Rect(centerPosition.x - 60f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                    if (prevButton.Contains(mousePos))
                    {
                        return -3;
                    }
                }

                if (currentPage < totalPages - 1)
                {
                    Rect nextButton = new Rect(centerPosition.x + 40f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                    if (nextButton.Contains(mousePos))
                    {
                        return -4;
                    }
                }
            }

            for (int i = 0; i < currentPageItems.Count; i++)
            {
                float angle = (360f / currentPageItems.Count) * i - 90f;
                Vector2 itemPos = GetItemPosition(angle);
                Rect itemRect = new Rect(itemPos.x - itemSize / 2f, itemPos.y - itemSize / 2f, itemSize, itemSize);

                if (itemRect.Contains(mousePos))
                {
                    return i;
                }
            }

            return -1;
        }

        private void HandleInput()
        {
            OnConfirmInput();
            OnGoBackInput();
            OnCloseInput();
        }

        private void OnGoBackInput()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                if (hoveredIndex >= 0 && hoveredIndex < currentPageItems.Count)
                {
                    RadialMenuItem item = currentPageItems[hoveredIndex];
                    if (item.IsFavoritable && sourcePawn != null)
                    {
                        FavoritesTracker.ToggleFavourite(sourcePawn, item.defName);
                        bool isFavorited = FavoritesTracker.IsFavourite(sourcePawn, item.defName);
                        Messages.Message($"{item.label} {(isFavorited ? "added to" : "removed from")} favorites.", MessageTypeDefOf.SilentInput);
                        Event.current.Use();
                        return;
                    }
                }

                if (menuStack.Count > 0)
                {
                    GoBack();
                }
                else
                {
                    Close();
                }
                Event.current.Use();
            }
        }

        private void OnConfirmInput()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (hoveredIndex == -2)
                {
                    GoBack();
                    Event.current.Use();
                }
                else if (hoveredIndex == -3)
                {
                    PreviousPage();
                    Event.current.Use();
                }
                else if (hoveredIndex == -4)
                {
                    NextPage();
                    Event.current.Use();
                }
                else if (hoveredIndex >= 0 && hoveredIndex < currentPageItems.Count)
                {
                    RadialMenuItem item = currentPageItems[hoveredIndex];
                    bool isEnabled = item.sourceGizmo?.Disabled != true;

                    if (isEnabled)
                    {
                        if (item.HasSubItems)
                        {
                            OpenSubmenu(item.subItems);
                        }
                        else if (item.action != null)
                        {
                            item.action();
                            Close();
                        }
                    }

                    Event.current.Use();
                }
            }
        }

        private void OnCloseInput()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (menuStack.Count > 0)
                {
                    GoBack();
                }
                else
                {
                    Close();
                }
                Event.current.Use();
            }
        }

        private void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                UpdateCurrentPageItems();
                hoveredIndex = -1;
            }
        }

        private void NextPage()
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                UpdateCurrentPageItems();
                hoveredIndex = -1;
            }
        }

        private void OpenSubmenu(List<RadialMenuItem> subItems)
        {
            menuStack.Push(allMenuItems);
            pageStack.Push(currentPage);
            allMenuItems = subItems;
            currentPage = 0;
            UpdateCurrentPageItems();
            hoveredIndex = -1;
        }

        private void GoBack()
        {
            if (menuStack.Count > 0)
            {
                allMenuItems = menuStack.Pop();
                currentPage = pageStack.Count > 0 ? pageStack.Pop() : 0;
                UpdateCurrentPageItems();
                hoveredIndex = -1;
            }
        }

        public static void Show(List<RadialMenuItem> menuItems, bool isFavoritesMenu = false)
        {
            RadialMenuWindow window = new RadialMenuWindow(menuItems, isFavoritesMenu);
            Find.WindowStack.Add(window);
            window.windowRect.x = UI.screenWidth / 2f - window.currentWindowSize.x / 2f;
            window.windowRect.y = (UI.screenHeight / 2f - window.currentWindowSize.y / 2f) - window.Settings.heightOffset;
        }

        public static void ShowFromGizmos(Pawn pawn, List<Command> abilityGizmos, bool isFavoritesMenu = false)
        {
            RadialMenuWindow window = new RadialMenuWindow(pawn, abilityGizmos, isFavoritesMenu);
            Find.WindowStack.Add(window);
            window.windowRect.x = UI.screenWidth / 2f - window.currentWindowSize.x / 2f;
            window.windowRect.y = (UI.screenHeight / 2f - window.currentWindowSize.y / 2f) - window.Settings.heightOffset;
        }
    }
}