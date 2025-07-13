using System.Collections.Generic;
using Verse;

namespace RWRadial
{
    public class GameComp_RadialFavouritesTracker : GameComponent
    {
        public Dictionary<Pawn, List<string>> PawnAbilityFavourites = new Dictionary<Pawn, List<string>>();

        private List<Pawn> workingKeysList;
        private List<List<string>> workingValuesList;

        public GameComp_RadialFavouritesTracker(Game game)
        {
        }

        public void AddFavourite(Pawn pawn, string defName)
        {
            if (!PawnAbilityFavourites.ContainsKey(pawn))
            {
                PawnAbilityFavourites[pawn] = new List<string>();
            }

            if (PawnAbilityFavourites[pawn] == null)
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
            if (!PawnAbilityFavourites.ContainsKey(pawn))
            {
                return false;
            }

            if (PawnAbilityFavourites[pawn] != null && PawnAbilityFavourites[pawn].Count > 0)
            {
                return true;
            }

            return false;
        }

        public void RemoveFavourite(Pawn pawn, string defName)
        {
            if (PawnAbilityFavourites.ContainsKey(pawn) && PawnAbilityFavourites[pawn] != null)
            {
                PawnAbilityFavourites[pawn].Remove(defName);
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
            if (PawnAbilityFavourites.ContainsKey(pawn) && PawnAbilityFavourites[pawn] != null)
            {
                return PawnAbilityFavourites[pawn].Contains(defName);
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
