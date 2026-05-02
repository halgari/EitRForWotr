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
        // PA/CE/DA toggles — only at BAB ≥ +1 (matches the feature's prereq).
        var freebies = Mutators.FreeCombatOptions.EitrFreebies;
        if (freebies != null
            && __instance.Stats?.BaseAttackBonus != null
            && __instance.Stats.BaseAttackBonus.BaseValue >= 1
            && !__instance.HasFact(freebies)) {
          __instance.AddFact(freebies);
        }

        // Finesse rules — no prereq, applies to everyone (the underlying
        // AttackStatReplacement only triggers when wielding a Finessable weapon).
        var finesse = Mutators.FinesseWeaponRules.EitrFinesse;
        if (finesse != null && !__instance.HasFact(finesse)) {
          __instance.AddFact(finesse);
        }
      } catch (System.Exception ex) {
        Main.Log.Error("UnitDescriptor_Init_Patch: " + ex);
      }
    }
  }
}
