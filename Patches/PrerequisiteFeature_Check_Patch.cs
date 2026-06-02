using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace EitRForWotr.Patches {
  /// <summary>
  /// Runtime safety net for prerequisite checks on lazily-loaded blueprints.
  ///
  /// StripAsPrerequisite / RedirectPrerequisite only reach blueprints that are
  /// loaded at mutation time (ForEachLoaded). Blueprints deserialized later
  /// retain their original prerequisite components. This patch intercepts the
  /// evaluation so the right answer is returned regardless of load order.
  /// </summary>
  internal static class PrerequisiteFeature_Check_Patch {
    internal static readonly HashSet<BlueprintGuid> BypassedPrereqs = new();
    internal static readonly Dictionary<BlueprintGuid, BlueprintFeature> Redirects = new();

    [HarmonyPatch(typeof(PrerequisiteFeature), "CheckInternal")]
    internal static class SingleFeature_Patch {
      static bool Prefix(PrerequisiteFeature __instance, ref bool __result, UnitDescriptor unit) {
        if (__instance.Feature == null) return true;
        var guid = __instance.Feature.AssetGuid;

        if (BypassedPrereqs.Contains(guid)) {
          __result = true;
          return false;
        }

        if (Redirects.TryGetValue(guid, out var replacement)) {
          __result = unit.HasFact(replacement);
          return false;
        }

        return true;
      }
    }

    [HarmonyPatch(typeof(PrerequisiteFeaturesFromList), "CheckInternal")]
    internal static class FeatureList_Patch {
      static bool Prefix(PrerequisiteFeaturesFromList __instance, ref bool __result) {
        if (BypassedPrereqs.Count == 0) return true;

        var features = __instance.Features;
        bool allBypassed = true;
        foreach (var feat in features) {
          if (feat == null) continue;
          if (!BypassedPrereqs.Contains(feat.AssetGuid)) {
            allBypassed = false;
            break;
          }
        }

        if (allBypassed) {
          __result = true;
          return false;
        }

        return true;
      }
    }
  }
}
