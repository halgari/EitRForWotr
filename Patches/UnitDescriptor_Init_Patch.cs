using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace EitRForWotr.Patches {
  /// <summary>
  /// Inject the EitR auto-granted features onto every unit. Two hooks:
  ///
  /// • UnitDescriptor.Initialize — fires when a unit is created (new
  ///   characters, freshly-spawned encounter NPCs/monsters).
  /// • UnitDescriptor.PostLoad — fires when a unit is deserialized from a
  ///   save. Without this, PCs and companions in saves predating the mod
  ///   never get the new features (Initialize doesn't re-fire on load).
  ///
  /// Both code paths share the same idempotent "AddFact if missing" logic,
  /// so applying the same hook twice on a unit (Init + PostLoad on a fresh
  /// game) is safe.
  /// </summary>
  internal static class UnitDescriptor_Init_Patch {
    private static void GrantEitrFeatures(UnitDescriptor unit) {
      try {
        // PA/CE/DA toggles — only at BAB ≥ +1 (matches the feature's prereq).
        var freebies = Mutators.FreeCombatOptions.EitrFreebies;
        if (freebies != null
            && unit.Stats?.BaseAttackBonus != null
            && unit.Stats.BaseAttackBonus.BaseValue >= 1
            && !unit.HasFact(freebies)) {
          unit.AddFact(freebies);
        }

        // Finesse rules — no prereq, applies to everyone (the underlying
        // AttackStatReplacement only triggers when wielding a Finessable weapon).
        var finesse = Mutators.FinesseWeaponRules.EitrFinesse;
        if (finesse != null && !unit.HasFact(finesse)) {
          unit.AddFact(finesse);
        }
      } catch (System.Exception ex) {
        Main.Log.Error("GrantEitrFeatures: " + ex);
      }
    }

    [HarmonyPatch(typeof(UnitDescriptor), nameof(UnitDescriptor.Initialize), new System.Type[0])]
    internal static class Initialize_Patch {
      static void Postfix(UnitDescriptor __instance) => GrantEitrFeatures(__instance);
    }

    [HarmonyPatch(typeof(UnitDescriptor), nameof(UnitDescriptor.PostLoad))]
    internal static class PostLoad_Patch {
      static void Postfix(UnitDescriptor __instance) => GrantEitrFeatures(__instance);
    }
  }
}
