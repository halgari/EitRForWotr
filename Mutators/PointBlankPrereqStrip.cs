using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR change #10 (selection/prereq side) — PBS is gone as a *selectable
  /// feat* and as a prerequisite. The +1/+1 ranged-within-30-ft EFFECT is
  /// auto-granted to everyone via FinesseWeaponRules; this mutator just
  /// strips PBS from selection lists and clears prereq references to it.
  ///
  /// Existing characters who took PBS keep it (it remains a no-op duplicate
  /// fact alongside the auto-granted version — same blueprint, no stacking).
  /// Class features that auto-grant PBS (e.g. Point-Blank Master, ranger
  /// Archery Style) are unaffected — they still grant the same blueprint.
  /// </summary>
  internal static class PointBlankPrereqStrip {
    public static void Apply() {
      var pbs = Helpers.Get<BlueprintFeature>(FeatureRefs.PointBlankShot.ToString());
      if (pbs == null) {
        Main.Log.Error("PointBlankPrereqStrip: PointBlankShot blueprint not found");
        return;
      }
      Helpers.RemoveFromAllSelections(pbs);
      Patches.PrerequisiteFeature_Check_Patch.BypassedPrereqs.Add(pbs.AssetGuid);
      Main.Log.Log("PointBlankPrereqStrip: stripped PBS from selections + prerequisites (#10)");
    }
  }
}
