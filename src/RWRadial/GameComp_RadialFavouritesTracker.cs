using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    public class GameComp_RadialFavouritesTracker : GameComponent
    {
        public Dictionary<Pawn, List<string>> PawnAbilityFavourites = new Dictionary<Pawn, List<string>>();
        private List<Pawn> workingKeysList;
        private List<List<string>> workingValuesList;

        public GameComp_RadialFavouritesTracker(Game game)
        {

        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            if (ContextMenuDefOf.RWR_OpenArchitectMenu.JustPressed && Find.CurrentMap != null && !Find.WindowStack.AnyWindowAbsorbingAllInput)
            {
                OpenArchitectRadialMenu();
            }
        }

        private void OpenArchitectRadialMenu()
        {
            List<ContextMenuItem> categoryItems = new List<ContextMenuItem>();

            foreach (var catDef in DefDatabase<DesignationCategoryDef>.AllDefs.OrderBy(c => c.order))
            {
                if (catDef.ResolvedAllowedDesignators.Any(d => d.Visible))
                {
                    var icon = catDef.ResolvedAllowedDesignators.First(d => d.Visible).icon;
                    var localCatDef = catDef;

                    var catItem = new ContextMenuItem(null, catDef.LabelCap, catDef.description, icon as Texture2D)
                    {
                        getSubItems = () => GetDesignatorItemsFor(localCatDef)
                    };
                    categoryItems.Add(catItem);
                }
            }
            UIContextMenuWindow.Show(categoryItems);
        }

        private List<ContextMenuItem> GetDesignatorItemsFor(DesignationCategoryDef categoryDef)
        {
            List<ContextMenuItem> designatorItems = new List<ContextMenuItem>();
            foreach (var designator in categoryDef.ResolvedAllowedDesignators.OrderBy(d => d.Order))
            {
                if (designator.Visible)
                {
                    designatorItems.Add(new ContextMenuItem(designator));
                }
            }
            return designatorItems;
        }


        public void AddFavourite(Pawn pawn, string defName)
        {
            if (!PawnAbilityFavourites.ContainsKey(pawn) || PawnAbilityFavourites[pawn] == null)
            {
                PawnAbilityFavourites[pawn] = new List<string>();
            }

            if (!PawnAbilityFavourites[pawn].Contains(defName))
            {
                PawnAbilityFavourites[pawn].Add(defName);
            }
        }

        public bool HasAnyFavourites(Pawn pawn)
        {
            if (PawnAbilityFavourites.TryGetValue(pawn, out List<string> favourites) && favourites != null)
            {
                return favourites.Count > 0;
            }
            return false;
        }

        public void RemoveFavourite(Pawn pawn, string defName)
        {
            if (PawnAbilityFavourites.TryGetValue(pawn, out List<string> favourites) && favourites != null)
            {
                favourites.Remove(defName);
            }
        }

        public void ToggleFavourite(Pawn pawn, string defName)
        {
            if (IsFavourite(pawn, defName))
            {
                RemoveFavourite(pawn, defName);
            }
            else
            {
                AddFavourite(pawn, defName);
            }
        }

        public bool IsFavourite(Pawn pawn, string defName)
        {
            if (PawnAbilityFavourites.TryGetValue(pawn, out List<string> favourites) && favourites != null)
            {
                return favourites.Contains(defName);
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref PawnAbilityFavourites, "pawnAbilityFavourites", LookMode.Reference, LookMode.Value, ref workingKeysList, ref workingValuesList);

            if (PawnAbilityFavourites == null)
            {
                PawnAbilityFavourites = new Dictionary<Pawn, List<string>>();
            }
        }
    }
}