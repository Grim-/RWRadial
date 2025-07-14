using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    public class ContextMenuLayoutDef : Def
    {
        public Type layoutWorker;
        public MenuLayout CreateWorker(UIContextMenuWindow menuWindow)
        {
            MenuLayout menuLayout = (MenuLayout)Activator.CreateInstance(layoutWorker, 
            new object[] {
            menuWindow
            });
            return menuLayout;
        }
    }


    [StaticConstructorOnStartup]
    public class UIContextMenuWindow : Window
    {
        internal List<ContextMenuItem> allMenuItems;
        internal List<ContextMenuItem> currentPageItems;
        internal Stack<List<ContextMenuItem>> menuStack;
        internal Stack<int> pageStack;
        internal Vector2 currentWindowSize;
        internal bool isFavoritesMenu;
        internal Pawn sourcePawn;
        internal int currentPage = 0;
        internal int hoveredIndex = -1;

        private MenuLayout layout;

        internal AbilityRadialPagerSettings Settings => RWRadialMod.Settings;
        internal GameComp_RadialFavouritesTracker FavoritesTracker => Current.Game.GetComponent<GameComp_RadialFavouritesTracker>();
        internal int itemsPerPage => layout.ItemsPerPage;
        internal int totalPages => Mathf.CeilToInt((float)allMenuItems.Count / itemsPerPage);
        internal bool hasMultiplePages => totalPages > 1;


        public static Texture2D BackgroundTex = ContentFinder<Texture2D>.Get("UI/RadialBG");
        public static Texture2D GridBackgroundTex = ContentFinder<Texture2D>.Get("UI/BoxBG");
        public static Texture2D ArrowRight = ContentFinder<Texture2D>.Get("UI/UIArrowforward");
        public static Texture2D ArrowLeft = ContentFinder<Texture2D>.Get("UI/UIArrowback");
        public static Texture2D NewClose = ContentFinder<Texture2D>.Get("UI/UIClose");


        public UIContextMenuWindow(List<ContextMenuItem> menuItems, bool isFavoritesMenu = false)
        {
            this.allMenuItems = menuItems.OrderBy(x => x.order).ToList();
            this.isFavoritesMenu = isFavoritesMenu;
            this.sourcePawn = menuItems.FirstOrDefault()?.parentPawn;
            this.menuStack = new Stack<List<ContextMenuItem>>();
            this.pageStack = new Stack<int>();
            this.currentPage = 0;

            this.layout = Settings.layoutDef != null ? Settings.layoutDef.CreateWorker(this) : new RadialMenuLayout(this);
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

        public UIContextMenuWindow(Pawn pawn, List<Command> abilityGizmos, bool isFavoritesMenu = false)
        {
            this.sourcePawn = pawn;
            this.allMenuItems = BuildAbilityMenuItems(pawn, abilityGizmos);
            this.isFavoritesMenu = isFavoritesMenu;
            this.menuStack = new Stack<List<ContextMenuItem>>();
            this.pageStack = new Stack<int>();
            this.currentPage = 0;

            this.layout = Settings.layoutDef != null ? Settings.layoutDef.CreateWorker(this) : new RadialMenuLayout(this);

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

        private Vector2 CalculateWindowSize() => layout.CalculateWindowSize();

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 mousePos = Event.current.mousePosition;
            hoveredIndex = layout.GetHoveredItemIndex(mousePos);

            Vector2 newSize = CalculateWindowSize();
            if (newSize != currentWindowSize)
            {
                ResizeWindow(newSize);
            }

            layout.DoLayout(inRect);

            HandleInput();
        }

        private List<ContextMenuItem> BuildAbilityMenuItems(Pawn pawn, List<Command> abilityGizmos)
        {
            var categoryGroups = abilityGizmos
                .GroupBy(g => AbilityRadialPager.GetAbilityCategory(g))
                .OrderBy(group => group.Key)
                .ToList();

            List<ContextMenuItem> menuItems = new List<ContextMenuItem>();

            foreach (var categoryGroup in categoryGroups)
            {
                var categoryItem = new ContextMenuItem(
                    pawn,
                    categoryGroup.Key,
                    "",
                    categoryGroup.First().icon as Texture2D
                );

                foreach (var abilityGizmo in categoryGroup)
                {
                    ContextMenuItem abilityItem = new ContextMenuItem(
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
                menuItems.Add(new ContextMenuItem(pawn, "Favourites", "Favourites", null, () => OpenFavoritesMenu(), 20));
            }

            return menuItems.OrderBy(x => x.order).ToList();
        }

        private void OpenFavoritesMenu()
        {
            if (sourcePawn == null) return;

            var favoriteDefNames = FavoritesTracker.PawnAbilityFavourites.ContainsKey(sourcePawn)
                ? FavoritesTracker.PawnAbilityFavourites[sourcePawn]
                : new List<string>();

            List<ContextMenuItem> favoriteItems = new List<ContextMenuItem>();

            foreach (var menuItem in GetAllAbilityItems(allMenuItems))
            {
                if (!string.IsNullOrEmpty(menuItem.defName) && favoriteDefNames.Contains(menuItem.defName))
                {
                    favoriteItems.Add(menuItem);
                }
            }

            if (favoriteItems.Any())
            {
                UIContextMenuWindow favWindow = new UIContextMenuWindow(favoriteItems, true);
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

        private List<ContextMenuItem> GetAllAbilityItems(List<ContextMenuItem> items)
        {
            List<ContextMenuItem> result = new List<ContextMenuItem>();
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

        private void HandleInput()
        {
            OnConfirmInput();
            OnGoBackInput();
            OnCloseInput();


            if (ContextMenuDefOf.RWR_NextMenu.KeyDownEvent)
            {
                NextPage();
            }
            else if (ContextMenuDefOf.RWR_PreviousMenu.KeyDownEvent)
            {
                PreviousPage();
            }
        }

        private void OnGoBackInput()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                if (hoveredIndex >= 0 && hoveredIndex < currentPageItems.Count)
                {
                    ContextMenuItem item = currentPageItems[hoveredIndex];
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
                    ContextMenuItem item = currentPageItems[hoveredIndex];
                    bool isEnabled = item.sourceGizmo?.Disabled != true;

                    if (isEnabled)
                    {
                        if (item.getSubItems != null)
                        {
                            OpenSubmenu(item.getSubItems());
                        }
                        else if (item.HasSubItems)
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
            if (ContextMenuDefOf.RWR_OpenMenu.KeyDownEvent)
            {
                if (menuStack.Count > 0)
                {
                    GoBack();
                }
                else
                {
                    Close();
                }
            }
            else
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
        }

        public void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                UpdateCurrentPageItems();
                hoveredIndex = -1;
            }
        }

        public void NextPage()
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                UpdateCurrentPageItems();
                hoveredIndex = -1;
            }
        }

        private void OpenSubmenu(List<ContextMenuItem> subItems)
        {
            menuStack.Push(allMenuItems);
            pageStack.Push(currentPage);
            allMenuItems = subItems;
            currentPage = 0;
            UpdateCurrentPageItems();
            hoveredIndex = -1;
        }

        public void GoBack()
        {
            if (menuStack.Count > 0)
            {
                allMenuItems = menuStack.Pop();
                currentPage = pageStack.Count > 0 ? pageStack.Pop() : 0;
                UpdateCurrentPageItems();
                hoveredIndex = -1;
            }
        }

        public static void Show(List<ContextMenuItem> menuItems, bool isFavoritesMenu = false)
        {
            UIContextMenuWindow window = new UIContextMenuWindow(menuItems, isFavoritesMenu);
            Find.WindowStack.Add(window);
            window.windowRect.x = UI.screenWidth / 2f - window.currentWindowSize.x / 2f;
            window.windowRect.y = (UI.screenHeight / 2f - window.currentWindowSize.y / 2f) - window.Settings.heightOffset;
        }

        public static void ShowFromGizmos(Pawn pawn, List<Command> abilityGizmos, bool isFavoritesMenu = false)
        {
            UIContextMenuWindow window = new UIContextMenuWindow(pawn, abilityGizmos, isFavoritesMenu);
            Find.WindowStack.Add(window);
            window.windowRect.x = UI.screenWidth / 2f - window.currentWindowSize.x / 2f;
            window.windowRect.y = (UI.screenHeight / 2f - window.currentWindowSize.y / 2f) - window.Settings.heightOffset;
        }
    }
}