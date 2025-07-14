using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    [StaticConstructorOnStartup]
    public class RadialMenuLayout : MenuLayout
    {

        private AbilityRadialPagerSettings Settings => window.Settings;
        private GameComp_RadialFavouritesTracker FavoritesTracker => window.FavoritesTracker;

        private Vector2 centerPosition => new Vector2(window.windowRect.size.x / 2, window.windowRect.size.y / 2 - Settings.heightOffset);
        private float SpacePerItem => Mathf.Lerp(Settings.maxSpacePerItem, Settings.minSpacePerItem,
            Mathf.InverseLerp(Settings.minPageCount, Settings.maxPageCount, window.currentPageItems.Count));
        private float baseRadius => Settings.baseRadius;
        private float radius => baseRadius + (window.currentPageItems.Count * SpacePerItem);
        private float itemSize => Mathf.Lerp(Settings.maxItemSize, Settings.minItemSize,
            Mathf.InverseLerp(Settings.minPageCount, Settings.maxPageCount, window.currentPageItems.Count));

        public override int ItemsPerPage => Settings.itemsPerPage;

        private float extraPageIndicatorHeight = 20f;

        public RadialMenuLayout(UIContextMenuWindow window) : base(window) { }

        public RadialMenuLayout()
        {

        }

        public override Vector2 CalculateWindowSize()
        {
            float menuRadius = radius;
            float labelHeight = Text.CalcHeight("Sample", 200f);
            float pageIndicatorHeight = window.hasMultiplePages ? extraPageIndicatorHeight : 0f;
            float totalRadius = menuRadius + itemSize / 2f + labelHeight + pageIndicatorHeight + 30f;
            float size = (totalRadius * 2f) + 10f;
            return new Vector2(size, size);
        }

        public override void DoLayout(Rect inRect)
        {
            if (UIContextMenuWindow.BackgroundTex != null)
            {
                float bgSize = (radius + itemSize / 2f) * 2f;
                Rect bgRect = new Rect(
                    centerPosition.x - bgSize / 2f,
                    centerPosition.y - bgSize / 2f,
                    bgSize,
                    bgSize
                );
                Widgets.DrawTextureFitted(bgRect, UIContextMenuWindow.BackgroundTex, 1);
            }

            DrawMenu(inRect);
        }

        private void DrawMenu(Rect rect)
        {
            for (int i = 0; i < window.currentPageItems.Count; i++)
            {
                DrawMenuItem(i, window.currentPageItems[i]);
            }

            if (window.menuStack.Count > 0)
            {
                Rect backButton = new Rect(centerPosition.x - Settings.backButtonSize / 2,
                    centerPosition.y - Settings.backButtonSize / 2, Settings.backButtonSize, Settings.backButtonSize);
                GUI.color = window.hoveredIndex == -2 ? Color.yellow : Color.white;

                if (window.hoveredIndex >= 0 && window.hoveredIndex < window.currentPageItems.Count && window.currentPageItems[window.hoveredIndex].icon != null)
                {
                    GUI.DrawTexture(backButton, window.currentPageItems[window.hoveredIndex].icon);
                }
                else
                {
                    GUI.DrawTexture(backButton, UIContextMenuWindow.NewClose);
                }
                GUI.color = Color.white;
            }

            if (window.hasMultiplePages)
            {
                DrawPageNavigation();
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
                float yOffset = window.hasMultiplePages ? 40f : 20f;
                Rect hoveredItemLabel = new Rect(centerPosition.x - labelSize.x / 2f, centerPosition.y + yOffset, labelSize.x, labelSize.y);
                GUI.Label(hoveredItemLabel, displayText);
            }

            if (window.isFavoritesMenu)
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
            string pageText = $"{window.currentPage + 1} / {window.totalPages}";
            Vector2 pageTextSize = Text.CalcSize(pageText);
            Rect pageTextRect = new Rect(centerPosition.x - pageTextSize.x / 2f, centerPosition.y + 20f, pageTextSize.x, pageTextSize.y);
            GUI.Label(pageTextRect, pageText);

            if (window.currentPage > 0)
            {
                Rect prevButton = new Rect(centerPosition.x - 60f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                GUI.color = window.hoveredIndex == -3 ? Color.yellow : Color.white;
                GUI.DrawTexture(prevButton, UIContextMenuWindow.ArrowLeft);
                GUI.color = Color.white;
            }

            if (window.currentPage < window.totalPages - 1)
            {
                Rect nextButton = new Rect(centerPosition.x + 40f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                GUI.color = window.hoveredIndex == -4 ? Color.yellow : Color.white;
                GUI.DrawTexture(nextButton, UIContextMenuWindow.ArrowRight);
                GUI.color = Color.white;
            }
        }

        private void DrawMenuItem(int index, ContextMenuItem item)
        {
            float angle = (360f / window.currentPageItems.Count) * index - 90f;
            Vector2 itemPos = GetItemPosition(angle);

            float extraHoverSize = index == window.hoveredIndex ? Settings.hoverSizeIncrease : 1f;

            Rect itemRect = new Rect(itemPos.x - itemSize * extraHoverSize / 2f, itemPos.y - itemSize * extraHoverSize / 2f,
                itemSize * extraHoverSize, itemSize * extraHoverSize);

            bool isEnabled = item.sourceGizmo?.Disabled != true;
            Color itemColor = isEnabled ? (item.color != Color.white ? item.color : Color.white) : Color.gray;

            GUI.color = itemColor;

            if (index == window.hoveredIndex)
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

            if (item.IsFavoritable && window.sourcePawn != null && FavoritesTracker.IsFavourite(window.sourcePawn, item.defName))
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

        public override int GetHoveredItemIndex(Vector2 mousePos)
        {
            if (window.menuStack.Count > 0)
            {
                Rect backButton = new Rect(centerPosition.x - Settings.backButtonSize / 2,
                    centerPosition.y - Settings.backButtonSize / 2, Settings.backButtonSize, Settings.backButtonSize);
                if (backButton.Contains(mousePos))
                {
                    return -2;
                }
            }

            if (window.hasMultiplePages)
            {
                if (window.currentPage > 0)
                {
                    Rect prevButton = new Rect(centerPosition.x - 60f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                    if (prevButton.Contains(mousePos))
                    {
                        return -3;
                    }
                }

                if (window.currentPage < window.totalPages - 1)
                {
                    Rect nextButton = new Rect(centerPosition.x + 40f, centerPosition.y + 18f, Settings.navButtonsSize, Settings.navButtonsSize);
                    if (nextButton.Contains(mousePos))
                    {
                        return -4;
                    }
                }
            }

            for (int i = 0; i < window.currentPageItems.Count; i++)
            {
                float angle = (360f / window.currentPageItems.Count) * i - 90f;
                Vector2 itemPos = GetItemPosition(angle);
                Rect itemRect = new Rect(itemPos.x - itemSize / 2f, itemPos.y - itemSize / 2f, itemSize, itemSize);

                if (itemRect.Contains(mousePos))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}