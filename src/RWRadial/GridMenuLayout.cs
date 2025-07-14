using System.Linq;
using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    [StaticConstructorOnStartup]
    public class GridMenuLayout : MenuLayout
    {
        private AbilityRadialPagerSettings Settings => window.Settings;
        private GameComp_RadialFavouritesTracker FavoritesTracker => window.FavoritesTracker;

        private readonly int maxItemsPerRow = 8;
        private readonly int maxRows = 3;
        private Vector2 itemSize = new Vector2(60, 60);
        private Vector2 itemPadding = new Vector2(15, 15);
        private float labelHeight = 35f;
        private float navigationHeight = 50f;
        private float headerHeight = 40f;
        private float backButtonSize = 40f;
        private float windowPadding = 75f;

        private float windowSizeExtraPaddingX = 60f;
        private float windowSizeExtraPaddingY = 40f;
        private float navAreaYOffset = 20f;
        private float backButtonXOffset = 20f;
        private float tooltipYOffset = 30f;
        private float navButtonDistanceFromText = 50f;
        private float navButtonYOffset = 30f;
        private float navButtonSize = 40f;
        private float navButtonXOffset = 20f;
        private float starSize = 12f;
        private float labelYOffset = 2f;
        private float subItemArrowSize = 8f;
        private float hoveredIndexNavYOffset = 10f;

        public override int ItemsPerPage => maxItemsPerRow * maxRows;

        public GridMenuLayout(UIContextMenuWindow window) : base(window)
        {

        }

        public override Vector2 CalculateWindowSize()
        {
            int itemCount = Mathf.Max(10, window.currentPageItems.Count);
            int itemsPerRow = Mathf.Min(itemCount, maxItemsPerRow);
            int numRows = Mathf.CeilToInt((float)itemCount / itemsPerRow);
            numRows = Mathf.Min(numRows, maxRows);

            float contentWidth = itemsPerRow * itemSize.x + (itemsPerRow - 1) * itemPadding.x;
            float contentHeight = numRows * itemSize.y + (numRows - 1) * itemPadding.y;

            if (Settings.showLabels)
            {
                contentHeight += numRows * labelHeight;
            }

            float totalWidth = contentWidth + (windowPadding * 2);
            float totalHeight = contentHeight + (windowPadding * 2);

            if (window.isFavoritesMenu)
            {
                totalHeight += headerHeight;
            }

            if (window.hasMultiplePages || window.menuStack.Any())
            {
                totalHeight += navigationHeight;
            }

            return new Vector2(totalWidth + windowSizeExtraPaddingX, totalHeight + windowSizeExtraPaddingY);
        }

        public override void DoLayout(Rect inRect)
        {
            if (UIContextMenuWindow.GridBackgroundTex != null)
            {
                Widgets.DrawTextureFitted(inRect, UIContextMenuWindow.GridBackgroundTex, 1);
            }
            DrawMenu(inRect);
        }

        private void DrawMenu(Rect rect)
        {
            float currentY = rect.y + windowPadding;

            if (window.isFavoritesMenu)
            {
                string favText = "Favorites Menu";
                Vector2 favTextSize = Text.CalcSize(favText);
                Rect favTextRect = new Rect(rect.center.x - favTextSize.x / 2f, currentY, favTextSize.x, favTextSize.y);
                GUI.color = Color.yellow;
                Widgets.Label(favTextRect, favText);
                GUI.color = Color.white;
                currentY += headerHeight;
            }

            int itemCount = window.currentPageItems.Count;
            int itemsPerRow = Mathf.Min(itemCount, maxItemsPerRow);
            int numRows = Mathf.CeilToInt((float)itemCount / itemsPerRow);

            float contentWidth = itemsPerRow * itemSize.x + (itemsPerRow - 1) * itemPadding.x;
            float startX = rect.center.x - contentWidth / 2f;

            for (int i = 0; i < itemCount; i++)
            {
                int row = i / itemsPerRow;
                int col = i % itemsPerRow;

                float x = startX + col * (itemSize.x + itemPadding.x);
                float y = currentY + row * (itemSize.y + itemPadding.y + (Settings.showLabels ? labelHeight : 0));

                DrawMenuItem(i, window.currentPageItems[i], new Rect(x, y, itemSize.x, itemSize.y));
            }

            if (window.hasMultiplePages || window.menuStack.Any())
            {
                string pageText = $"{window.currentPage + 1} / {window.totalPages}";
                Vector2 pageTextSize = Text.CalcSize(pageText);
                Rect pageTextRect = new Rect(rect.center.x - pageTextSize.x / 2f, rect.yMax - pageTextSize.y - 40, pageTextSize.x * 2, pageTextSize.y + 10);
                Text.Font = GameFont.Medium;
                Widgets.Label(pageTextRect, pageText);
                Text.Font = GameFont.Tiny;
                float navY = rect.yMax - navigationHeight + navAreaYOffset;
                DrawNavigationArea(new Rect(rect.x, navY, rect.width, navigationHeight));
            }

            if (window.hoveredIndex >= 0 && window.hoveredIndex < window.currentPageItems.Count)
            {
                ContextMenuItem hoveredItem = window.currentPageItems[window.hoveredIndex];
                bool isEnabled = hoveredItem.sourceGizmo?.Disabled != true;
                string displayText = isEnabled ? hoveredItem.label : $"{hoveredItem.label} ({hoveredItem.sourceGizmo?.disabledReason ?? "Disabled"})";

                if (hoveredItem.IsFavoritable && window.sourcePawn != null)
                {
                    bool isFavorited = FavoritesTracker.IsFavourite(window.sourcePawn, hoveredItem.defName);
                    displayText += isFavorited ? " ★" : " (Right-click to favorite)";
                }

                Vector2 labelSize = Text.CalcSize(displayText);
                float tooltipY = rect.yMax - navigationHeight - tooltipYOffset;
                Rect hoveredItemLabel = new Rect(rect.center.x - labelSize.x / 2f, tooltipY, labelSize.x, labelSize.y);
                GUI.Label(hoveredItemLabel, displayText);
            }
        }

        private void DrawNavigationArea(Rect navRect)
        {
            if (window.menuStack.Count > 0)
            {
                Rect backButtonRect = new Rect(navRect.center.x - navButtonSize / 2f, navRect.center.y - navButtonYOffset, navButtonSize, navButtonSize);
                GUI.color = window.hoveredIndex == -2 ? Color.yellow : Color.white;
                GUI.DrawTexture(backButtonRect, UIContextMenuWindow.NewClose);
                GUI.color = Color.white;
            }

            if (window.hasMultiplePages)
            {
                string pageText = $"{window.currentPage + 1} / {window.totalPages}";
                Vector2 pageTextSize = Text.CalcSize(pageText);

                if (window.currentPage > 0)
                {
                    Rect prevButtonRect = new Rect(navRect.center.x - pageTextSize.x / 2f - navButtonDistanceFromText, navRect.center.y - navButtonYOffset, navButtonSize, navButtonSize);
                    GUI.color = window.hoveredIndex == -3 ? Color.yellow : Color.white;
                    GUI.DrawTexture(prevButtonRect, UIContextMenuWindow.ArrowLeft);
                    GUI.color = Color.white;
                }

                if (window.currentPage < window.totalPages - 1)
                {
                    Rect nextButtonRect = new Rect(navRect.center.x + pageTextSize.x / 2f + navButtonXOffset, navRect.center.y - navButtonYOffset, navButtonSize, navButtonSize);
                    GUI.color = window.hoveredIndex == -4 ? Color.yellow : Color.white;
                    GUI.DrawTexture(nextButtonRect, UIContextMenuWindow.ArrowRight);
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawMenuItem(int index, ContextMenuItem item, Rect itemRect)
        {
            bool isEnabled = item.sourceGizmo?.Disabled != true;
            Color itemColor = isEnabled ? (item.color != Color.white ? item.color : Color.white) : Color.gray;

            if (index == window.hoveredIndex)
            {
                Widgets.DrawHighlight(itemRect);
                string tooltip = isEnabled ? item.description : $"{item.description}\n\nDisabled: {item.sourceGizmo?.disabledReason ?? "Unknown reason"}";
                TooltipHandler.TipRegion(itemRect, tooltip);
            }

            GUI.color = itemColor;
            GUI.DrawTexture(itemRect, item.icon ?? TexButton.Infinity);
            GUI.color = Color.white;

            if (item.IsFavoritable && window.sourcePawn != null && FavoritesTracker.IsFavourite(window.sourcePawn, item.defName))
            {
                Rect starRect = new Rect(itemRect.xMax - starSize, itemRect.y, starSize, starSize);
                GUI.color = Color.yellow;
                GUI.Label(starRect, "★");
                GUI.color = Color.white;
            }

            if (Settings.showLabels)
            {
                Rect labelRect = new Rect(itemRect.x, itemRect.yMax + labelYOffset, itemRect.width, labelHeight);
                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(labelRect, item.label);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (item.HasSubItems)
            {
                Rect arrowRect = new Rect(itemRect.xMax - subItemArrowSize, itemRect.y, subItemArrowSize, subItemArrowSize);
                GUI.DrawTexture(arrowRect, BaseContent.WhiteTex);
            }
        }

        public override int GetHoveredItemIndex(Vector2 mousePos)
        {
            Rect localBounds = new Rect(0, 0, window.windowRect.width, window.windowRect.height);

            if (window.hasMultiplePages || window.menuStack.Any())
            {
                float navY = localBounds.yMax - navigationHeight - hoveredIndexNavYOffset;
                Rect navArea = new Rect(localBounds.x, navY, localBounds.width, navigationHeight);

                if (window.menuStack.Count > 0)
                {
                    Rect backButtonRect = new Rect(navArea.center.x - navButtonSize / 2f, navArea.center.y - navButtonSize / 2f + 5f, navButtonSize, navButtonSize);
                    if (backButtonRect.Contains(mousePos)) return -2;
                }

                if (window.hasMultiplePages)
                {
                    string pageText = $"{window.currentPage + 1} / {window.totalPages}";
                    Vector2 pageTextSize = Text.CalcSize(pageText);

                    if (window.currentPage > 0)
                    {
                        Rect prevButtonRect = new Rect(navArea.center.x - pageTextSize.x / 2f - navButtonDistanceFromText, navArea.center.y - navButtonYOffset, navButtonSize, navButtonSize);
                        if (prevButtonRect.Contains(mousePos)) return -3;
                    }

                    if (window.currentPage < window.totalPages - 1)
                    {
                        Rect nextButtonRect = new Rect(navArea.center.x + pageTextSize.x / 2f + navButtonXOffset, navArea.center.y - navButtonYOffset, navButtonSize, navButtonSize);
                        if (nextButtonRect.Contains(mousePos)) return -4;
                    }
                }
            }

            float currentY = localBounds.y + windowPadding;
            if (window.isFavoritesMenu)
            {
                currentY += headerHeight;
            }

            int itemCount = window.currentPageItems.Count;
            int itemsPerRow = Mathf.Min(itemCount, maxItemsPerRow);
            float contentWidth = itemsPerRow * itemSize.x + (itemsPerRow - 1) * itemPadding.x;
            float startX = localBounds.center.x - contentWidth / 2f;

            for (int i = 0; i < itemCount; i++)
            {
                int row = i / itemsPerRow;
                int col = i % itemsPerRow;

                float x = startX + col * (itemSize.x + itemPadding.x);
                float y = currentY + row * (itemSize.y + itemPadding.y + (Settings.showLabels ? labelHeight : 0));

                Rect itemRect = new Rect(x, y, itemSize.x, itemSize.y);
                if (itemRect.Contains(mousePos))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}