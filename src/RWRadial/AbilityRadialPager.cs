using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RWGizmoMenu
{
    [StaticConstructorOnStartup]
    public static class AbilityRadialPager
    {
        private static Texture2D RadialIcon => TexCommand.Draft;
        private static AbilityRadialPagerSettings Settings => RWRadialMod.Settings;

        static AbilityRadialPager()
        {
            var harmony = new Harmony("com.emo.radialmenu");
            harmony.Patch(
                original: AccessTools.Method(typeof(GizmoGridDrawer), "DrawGizmoGrid"),
                prefix: new HarmonyMethod(typeof(AbilityRadialPager), nameof(GizmoGridPatchPrefix))
            );
        }

        public static bool GizmoGridPatchPrefix(ref IEnumerable<Gizmo> gizmos, float startX, out Gizmo mouseoverGizmo,
            Func<Gizmo, bool> customActivatorFunc, Func<Gizmo, bool> highlightFunc, Func<Gizmo, bool> lowlightFunc, bool multipleSelected)
        {
            mouseoverGizmo = null;
            if (Event.current.type == EventType.Layout || Find.CurrentMap == null || (Settings != null && !Settings.IsEnabled))
                return true;

            if (!(Find.Selector.SingleSelectedObject is Pawn) || Find.Selector.SelectedPawns.Count > 1)
                return true;

            Pawn selectedPawn = Find.Selector.SelectedPawns[0];
            var gizmoList = gizmos.ToList();


            var favoritesTracker = Current.Game.GetComponent<GameComp_RadialFavouritesTracker>();
            var favoriteDefNames = favoritesTracker.PawnAbilityFavourites.ContainsKey(selectedPawn)
                ? favoritesTracker.PawnAbilityFavourites[selectedPawn]
                : new List<string>();

            var nonAbilityGizmos = new List<Gizmo>();
            var tmfAbilityGizmos = new List<Command>();
            var standardAbilityGizmos = new List<Command>();

            foreach (var gizmo in gizmoList)
            {
                if (Settings.ShowFavouritesOnMainBar && IsAbilityGizmo(gizmo))
                {
                    string defName = GetAbilityDefName(gizmo as Command);
                    if (!string.IsNullOrEmpty(defName) && favoriteDefNames.Contains(defName))
                    {
                        nonAbilityGizmos.Add(gizmo);
                        continue; 
                    }
                }

                if (TMFAbilityHelper.IsTMFLoaded && TMFAbilityHelper.IsTMFCommand(gizmo))
                {
                    tmfAbilityGizmos.Add(gizmo as Command);
                }
                else if (gizmo is Command_Ability commandAbility)
                {
                    standardAbilityGizmos.Add(commandAbility);
                }
                else
                {
                    nonAbilityGizmos.Add(gizmo);
                }
            }

            var allAbilityGizmos = new List<Command>();
            allAbilityGizmos.AddRange(standardAbilityGizmos);
            allAbilityGizmos.AddRange(tmfAbilityGizmos);

            if (allAbilityGizmos.Any())
            {
                var radialGizmo = CreateRadialMenuGizmo(selectedPawn, allAbilityGizmos);
                nonAbilityGizmos.Add(radialGizmo);
            }


            gizmos = nonAbilityGizmos;
            return true;
        }

        private static bool IsAbilityGizmo(Gizmo gizmo)
        {
            return gizmo is Command_Ability || (TMFAbilityHelper.IsTMFLoaded && TMFAbilityHelper.IsTMFCommand(gizmo));
        }

        private static Command_Action CreateRadialMenuGizmo(Pawn pawn, List<Command> abilityGizmos)
        {
            return new Command_Action
            {
                defaultLabel = "Abilities",
                defaultDesc = "Open radial ability menu",
                icon = RadialIcon,
                hotKey = ContextMenuDefOf.RWR_OpenMenu,
                action = () => OpenRadialMenu(pawn, abilityGizmos),
                Order = -100
            };
        }

        private static void OpenRadialMenu(Pawn pawn, List<Command> abilityGizmos)
        {
            if (abilityGizmos.Any())
            {
                UIContextMenuWindow.ShowFromGizmos(pawn, abilityGizmos, false);
            }
        }

        private static string GetGizmoLabel(Command gizmo) => gizmo.Label;

        public static string GetAbilityDescription(Command command)
        {
            if (TMFAbilityHelper.IsTMFLoaded && TMFAbilityHelper.IsTMFCommand(command))
            {
                return TMFAbilityHelper.GetTMFDescription(command);
            }
            if (command is Command_Ability commandAbi)
            {
                return commandAbi.Ability?.def?.description ?? "";
            }
            return "";
        }

        public static void ExecuteAbilityGizmo(Command abilityGizmo)
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

        public static string GetAbilityCategory(Command command)
        {
            if (TMFAbilityHelper.IsTMFLoaded && TMFAbilityHelper.IsTMFCommand(command))
            {
                return TMFAbilityHelper.GetTMFAbilityTreeLabel(command);
            }

            if (command is Command_Ability commandAbi)
            {
                return commandAbi.Ability?.def?.category?.defName ?? "Base Game";
            }
            return "Unknown";
        }

        public static string GetAbilityDefName(Command command)
        {
            if (TMFAbilityHelper.IsTMFLoaded && TMFAbilityHelper.IsTMFCommand(command))
            {
                return TMFAbilityHelper.GetTMFDefName(command);
            }
            if (command is Command_Ability commandAbi)
            {
                return commandAbi.Ability?.def?.defName ?? "";
            }
            return "";
        }
    }
}