using EitRForWotr.Components;
using HarmonyLib;
using Kingmaker.RuleSystem.Rules;

namespace EitRForWotr.Patches {
  /// <summary>
  /// Postfix on <c>RuleCalculateBaseCMB.OnTrigger</c> that relabels the
  /// hardcoded "Strength" modifier when a CMB-stat swap fires — so the
  /// combat log shows "Dexterity" (or whatever <c>ReplaceStrength</c> is
  /// set to) instead of the misleading "Strength". See
  /// <see cref="CmbLabelRewriter"/> for the rewrite logic.
  /// </summary>
  [HarmonyPatch(typeof(RuleCalculateBaseCMB), nameof(RuleCalculateBaseCMB.OnTrigger))]
  internal static class RuleCalculateBaseCMB_OnTrigger_Patch {
    static void Postfix(RuleCalculateBaseCMB __instance) =>
      CmbLabelRewriter.RewriteForReplacement(__instance);
  }
}
