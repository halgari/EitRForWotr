using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR change #10 — Point-Blank Shot stays selectable but is no longer a
  /// prerequisite for Precise Shot or any other feat. Per Iantorno's clarification:
  /// "Point Blank Shot still exists. However, it is no longer a prerequisite for
  /// Precise Shot."
  /// </summary>
  internal static class PointBlankPrereqStrip {
    public static void Apply() {
      var pbs = Helpers.Get<BlueprintFeature>(FeatureRefs.PointBlankShot.ToString());
      if (pbs == null) {
        Main.Log.Error("PointBlankPrereqStrip: PointBlankShot blueprint not found");
        return;
      }
      Helpers.StripAsPrerequisite(pbs);
      Main.Log.Log("PointBlankPrereqStrip: stripped PBS from all prerequisites (#10)");
    }
  }
}
