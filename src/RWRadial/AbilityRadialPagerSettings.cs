using Verse;

namespace RWRadial
{
    public class AbilityRadialPagerSettings : ModSettings
    {
        public bool IsEnabled = true;

        public bool ShowFavouritesMenu = true;
        public bool ShowFavouritesOnMainBar = true;


        public float baseRadius = 50f;
        public int itemsPerPage = 12;

        public float minItemSize = 32f;
        public float maxItemSize = 50f;
        public float minSpacePerItem = 4f;
        public float maxSpacePerItem = 12f;
        public float heightOffset = 50f;
        public bool showLabels = true;
        public float hoverSizeIncrease = 1.2f;
        public float backButtonSize = 32f;
        public float navButtonsSize = 20f;
        public int minPageCount = 3;
        public int maxPageCount = 50;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IsEnabled, "IsEnabled", true);
            Scribe_Values.Look(ref ShowFavouritesMenu, "ShowFavouritesMenu", true);
            Scribe_Values.Look(ref ShowFavouritesOnMainBar, "ShowFavouritesOnMainBar", true);
            Scribe_Values.Look(ref itemsPerPage, "itemsPerPage", 8);
            Scribe_Values.Look(ref minItemSize, "minItemSize", 32f);
            Scribe_Values.Look(ref baseRadius, "baseRadius", 50f);
            Scribe_Values.Look(ref maxItemSize, "maxItemSize", 64f);
            Scribe_Values.Look(ref minSpacePerItem, "minSpacePerItem", 4f);
            Scribe_Values.Look(ref maxSpacePerItem, "maxSpacePerItem", 12f);
            Scribe_Values.Look(ref heightOffset, "heightOffset", 50f);
            Scribe_Values.Look(ref showLabels, "showLabels", true);
            Scribe_Values.Look(ref hoverSizeIncrease, "hoverSizeIncrease", 1.2f);
            Scribe_Values.Look(ref backButtonSize, "backButtonSize", 32f);
            Scribe_Values.Look(ref navButtonsSize, "navButtonsSize", 20f);
            Scribe_Values.Look(ref minPageCount, "minPageCount", 3);
            Scribe_Values.Look(ref maxPageCount, "maxPageCount", 12);
        }

        public void ResetToDefaults()
        {
            IsEnabled = true;
            ShowFavouritesMenu = true;
            ShowFavouritesOnMainBar = true;
            itemsPerPage = 12;
            minItemSize = 32f;
            maxItemSize = 50f;
            minSpacePerItem = 4f;
            maxSpacePerItem = 12f;
            heightOffset = 50f;
            showLabels = true;
            hoverSizeIncrease = 1.2f;
            backButtonSize = 32f;
            navButtonsSize = 20f;
            minPageCount = 3;
            maxPageCount = 50;
        }
    }
}