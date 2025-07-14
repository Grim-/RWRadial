using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    public class ContextMenuItem
    {
        public Pawn parentPawn;
        public string label;
        public string description;
        public Texture2D icon;
        public Color color = Color.white;
        public Action action;
        public List<ContextMenuItem> subItems = new List<ContextMenuItem>();
        public Func<List<ContextMenuItem>> getSubItems;
        public Command sourceGizmo;
        public string defName;
        public int order = 0;

        public bool HasSubItems => subItems.Any() || getSubItems != null;
        public bool IsFavoritable => sourceGizmo != null && !string.IsNullOrEmpty(defName);

        public ContextMenuItem(Pawn pawn, string label, string description, Texture2D icon, Action action = null, int order = 0)
        {
            this.parentPawn = pawn;
            this.label = label;
            this.description = description;
            this.icon = icon;
            this.action = action;
            this.order = order;
        }

        public ContextMenuItem(Designator designator)
        {
            this.label = designator.LabelCap;
            this.description = designator.Desc;
            this.icon = designator.icon as Texture2D;
            this.action = () => {
                if (!designator.Disabled)
                {
                    Find.DesignatorManager.Select(designator);
                }
            };
            this.order = (int)designator.Order;
        }
    }
}