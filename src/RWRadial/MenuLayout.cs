using UnityEngine;

namespace RWGizmoMenu
{
    public abstract class MenuLayout
    {
        protected UIContextMenuWindow window;

        public abstract int ItemsPerPage { get; }

        protected MenuLayout()
        {

        }

        public MenuLayout(UIContextMenuWindow window)
        {
            this.window = window;
        }

        public abstract Vector2 CalculateWindowSize();
        public abstract void DoLayout(Rect inRect);
        public abstract int GetHoveredItemIndex(Vector2 mousePos);
    }
}