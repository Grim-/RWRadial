using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    public class RWRadialMod : Mod
    {
        private AbilityRadialPagerSettings settings;
        private Vector2 scrollPosition;

        public RWRadialMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<AbilityRadialPagerSettings>();
        }

        public static AbilityRadialPagerSettings Settings => LoadedModManager.GetMod<RWRadialMod>().GetSettings<AbilityRadialPagerSettings>();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            var scrollRect = new Rect(0f, 0f, inRect.width - 20f, inRect.height * 2f);
            var viewRect = new Rect(0f, 0f, scrollRect.width - 16f, 1200f);

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            listingStandard.Begin(viewRect);

            Text.Font = GameFont.Medium;
            listingStandard.Label("Ability Radial Pager Settings");
            Text.Font = GameFont.Small;
            listingStandard.Gap();

            listingStandard.CheckboxLabeled("Enable Menu", ref settings.IsEnabled);
            listingStandard.Gap(4f);

            listingStandard.CheckboxLabeled("Enable Menu", ref settings.IsEnabled);
            listingStandard.Gap(4f);

            listingStandard.CheckboxLabeled("Show Favourites In Submenu", ref settings.ShowFavouritesMenu);
            listingStandard.Gap(4f);


            listingStandard.CheckboxLabeled("Show Favourites On Main", ref settings.ShowFavouritesOnMainBar);
            listingStandard.Gap(4f);

            listingStandard.Label("Layout Selection");
            listingStandard.Gap(6f);

            if (listingStandard.ButtonText(settings.layoutDef?.LabelCap ?? "Select Layout"))
            {
                var options = new List<FloatMenuOption>();
                foreach (var layoutDef in DefDatabase<ContextMenuLayoutDef>.AllDefs)
                {
                    options.Add(new FloatMenuOption(layoutDef.LabelCap, () => settings.layoutDef = layoutDef));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }


            listingStandard.Label($"Radius: {settings.baseRadius}");
            settings.baseRadius = listingStandard.Slider(settings.baseRadius, 20f, 100f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Items per page: {settings.itemsPerPage}");
            settings.itemsPerPage = Mathf.RoundToInt(listingStandard.Slider(settings.itemsPerPage, 1, 50));
            listingStandard.Gap(4f);

            listingStandard.Label($"Minimum item size: {settings.minItemSize:F1}");
            settings.minItemSize = listingStandard.Slider(settings.minItemSize, 20f, 100f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Maximum item size: {settings.maxItemSize:F1}");
            settings.maxItemSize = listingStandard.Slider(settings.maxItemSize, settings.minItemSize, 120f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Minimum space per item: {settings.minSpacePerItem:F1}");
            settings.minSpacePerItem = listingStandard.Slider(settings.minSpacePerItem, 2f, 15f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Maximum space per item: {settings.maxSpacePerItem:F1}");
            settings.maxSpacePerItem = listingStandard.Slider(settings.maxSpacePerItem, settings.minSpacePerItem, 20f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Height offset: {settings.heightOffset:F1}");
            settings.heightOffset = listingStandard.Slider(settings.heightOffset, 0f, 100f);
            listingStandard.Gap();

            listingStandard.Label("Visual Settings");
            listingStandard.Gap(6f);

            listingStandard.CheckboxLabeled("Show item labels", ref settings.showLabels);
            listingStandard.Gap(4f);

            listingStandard.Label($"Hover size increase: {settings.hoverSizeIncrease:F2}x");
            settings.hoverSizeIncrease = listingStandard.Slider(settings.hoverSizeIncrease, 1f, 2f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Back button size: {settings.backButtonSize:F1}");
            settings.backButtonSize = listingStandard.Slider(settings.backButtonSize, 16f, 64f);
            listingStandard.Gap(4f);

            listingStandard.Label($"Navigation button size: {settings.navButtonsSize:F1}");
            settings.navButtonsSize = listingStandard.Slider(settings.navButtonsSize, 12f, 40f);
            listingStandard.Gap();

            listingStandard.Label("Page Count Limits");
            listingStandard.Gap(6f);

            listingStandard.Label($"Minimum page count: {settings.minPageCount}");
            settings.minPageCount = Mathf.RoundToInt(listingStandard.Slider(settings.minPageCount, 1, 10));
            listingStandard.Gap(4f);

            listingStandard.Label($"Maximum page count: {settings.maxPageCount}");
            settings.maxPageCount = Mathf.RoundToInt(listingStandard.Slider(settings.maxPageCount, settings.minPageCount, 50));
            listingStandard.Gap();

            listingStandard.Gap();

            if (listingStandard.ButtonText("Reset to Defaults"))
            {
                settings.ResetToDefaults();
            }

            listingStandard.Gap();
            listingStandard.Label("Note: Changes will take effect immediately for new radial menus.");

            listingStandard.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
        {
            return "Rimworld Gizmo Menu";
        }
    }
}
