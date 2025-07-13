using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RWRadial
{
    public class RadialMenuItem
    {
        public string label;
        public string description;
        public Texture2D icon;
        public Action action;
        public List<RadialMenuItem> subItems;
        public Color color = Color.white;
        public string abilityId;
        public Pawn parentPawn;
        public Command sourceGizmo;
        public string defName;

        public int order = 100;

        public RadialMenuItem(Pawn pawn, string label, string description = "", Texture2D icon = null, Action action = null, int order = 100)
        {
            this.parentPawn = pawn;
            this.label = label;
            this.description = description;
            this.icon = icon;
            this.action = action;
            this.order = order;
            this.subItems = new List<RadialMenuItem>();
        }

        public bool HasSubItems => subItems != null && subItems.Count > 0;
        public bool IsFavoritable => !string.IsNullOrEmpty(defName) && sourceGizmo != null;
    }
}
