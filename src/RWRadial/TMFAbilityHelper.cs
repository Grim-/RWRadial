using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Verse;

namespace RWRadial
{
    public static class TMFAbilityHelper
    {
        private static readonly Type TMFCommandAbilityInterface;
        private static readonly PropertyInfo AbilityProperty;
        private static readonly FieldInfo DefField;
        private static readonly FieldInfo AbilityTreesField;

        static TMFAbilityHelper()
        {
            TMFCommandAbilityInterface = AccessTools.TypeByName("TaranMagicFramework.CommandAbility");

            if (TMFCommandAbilityInterface == null)
                return;

            AbilityProperty = AccessTools.Property(TMFCommandAbilityInterface, "Ability");
            if (AbilityProperty == null) return;

            Type abilityType = AbilityProperty.PropertyType;
            DefField = AccessTools.Field(abilityType, "def");
            if (DefField == null) return;

            Type defType = DefField.FieldType;
            AbilityTreesField = AccessTools.Field(defType, "abilityTrees");
        }

        private static bool? isTmfLoaded;
        public static bool IsTMFLoaded
        {
            get
            {
                if (isTmfLoaded == null)
                {
                    isTmfLoaded = ModLister.GetActiveModWithIdentifier("Taranchuk.TaranMagicFramework") != null;
                }
                return isTmfLoaded.Value;
            }
        }

        public static bool IsTMFCommand(Gizmo gizmo)
        {
            if (!IsTMFLoaded || gizmo == null || TMFCommandAbilityInterface == null)
                return false;

            return TMFCommandAbilityInterface.IsAssignableFrom(gizmo.GetType());
        }

        private static object GetTMFAbility(object command)
        {
            if (command == null || AbilityProperty == null) return null;
            return AbilityProperty.GetValue(command);
        }

        private static object GetTMFDef(object command)
        {
            object ability = GetTMFAbility(command);
            if (ability == null || DefField == null) return null;
            return DefField.GetValue(ability);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static string GetTMFDefName(Gizmo gizmo)
        {
            if (!IsTMFLoaded || !IsTMFCommand(gizmo)) return null;

            object def = GetTMFDef(gizmo);
            return (def as Def)?.defName;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static string GetTMFDescription(Gizmo gizmo)
        {
            if (!IsTMFLoaded || !IsTMFCommand(gizmo)) return null;

            object def = GetTMFDef(gizmo);
            return (def as Def)?.description;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static string GetTMFAbilityTreeLabel(Gizmo gizmo)
        {
            if (!IsTMFLoaded || !IsTMFCommand(gizmo)) return "TMF";

            if (AbilityTreesField == null) return "TMF (No Tree)";

            object def = GetTMFDef(gizmo);
            if (def == null) return "TMF (No Def)";

            var trees = AbilityTreesField.GetValue(def) as IEnumerable;
            if (trees == null) return "TMF (Tree Null)";

            var firstTree = trees.Cast<object>().FirstOrDefault();
            if (firstTree == null) return "TMF (Tree Empty)";

            var labelProp = AccessTools.Property(firstTree.GetType(), "label");
            return labelProp?.GetValue(firstTree) as string ?? "TMF (No Label)";
        }
    }
}