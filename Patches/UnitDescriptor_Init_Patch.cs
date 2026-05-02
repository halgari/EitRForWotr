using HarmonyLib;
using Kingmaker.UnitLogic;

namespace EitRForWotr.Patches {
  /// <summary>
  /// Most NPCs/monsters don't get BasicFeatsProgression, so a hook on
  /// UnitDescriptor.Initialize is the cheapest way to inject the EitR free
  /// combat options onto every spawned unit with BAB ≥ +1.
  /// </summary>
  [HarmonyPatch(typeof(UnitDescriptor), nameof(UnitDescriptor.Initialize), new System.Type[0])]
  internal static class UnitDescriptor_Init_Patch {
    static void Postfix(UnitDescriptor __instance) {
      try {
        var feat = Mutators.FreeCombatOptions.EitrFreebies;
        if (feat == null) return;
        if (__instance.Stats?.BaseAttackBonus == null) return;
        if (__instance.Stats.BaseAttackBonus.BaseValue < 1) return;
        if (__instance.HasFact(feat)) return;
        __instance.AddFact(feat);
      } catch (System.Exception ex) {
        Main.Log.Error("UnitDescriptor_Init_Patch: " + ex);
      }
    }
  }
}
