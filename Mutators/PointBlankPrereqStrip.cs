using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR change #10 — Point-Blank Shot is gone. Per the original 2012 blog:
  /// "Gone. Precise Shot replaces it as a prerequisite for further archery feats."
  ///
  /// Unlike PA/CE/DA there's no "now a combat option" auto-grant — PBS just
  /// goes away. Existing characters who took it keep it (saves preserve GUIDs);
  /// nobody new can take it; nothing requires it as a prereq anymore.
  ///
  /// (Iantorno's later comment that "Point Blank Shot still exists" refers to
  /// the expanded PDF rules document, NOT this blog post — and we're scoped
  /// to the 2012 blog, not the PDF.)
  /// </summary>
  internal static class PointBlankPrereqStrip {
    public static void Apply() {
      var pbs = Helpers.Get<BlueprintFeature>(FeatureRefs.PointBlankShot.ToString());
      if (pbs == null) {
        Main.Log.Error("PointBlankPrereqStrip: PointBlankShot blueprint not found");
        return;
      }
      Helpers.RemoveFromAllSelections(pbs);
      Main.Log.Log("PointBlankPrereqStrip: stripped PBS from selections + prerequisites (#10)");
    }
  }
}
