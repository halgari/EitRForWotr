using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints.Classes;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR changes #5/#6/#8/#9 — consolidate Improved Trip / Disarm / Dirty Trick
  /// into a single Dex-13 "Deft Maneuvers" feat, and Improved Bull Rush / Sunder /
  /// Overrun into a Str-13 "Powerful Maneuvers" feat. The Greater-X feats now
  /// require the new merged feat instead of the per-maneuver Improved version.
  ///
  /// Note: WOTR doesn't implement Feint / Reposition / Steal / Drag as feats —
  /// the Iantorno spec mentions them but they're a no-op for our scope.
  ///
  /// Implementation pattern: each new feat AddFacts the existing Improved-X
  /// features (which carry the actual no-AoO + maneuver-bonus components).
  /// We don't reimplement combat-maneuver mechanics ourselves.
  /// </summary>
  internal static class ManeuverConsolidation {
    private const string DeftManeuversName = "EitRDeftManeuvers";
    private const string DeftManeuversGuid = "2866e67d11604907acf823744e1a8bd4";

    private const string PowerfulManeuversName = "EitRPowerfulManeuvers";
    private const string PowerfulManeuversGuid = "c63ec4d50d7242938addcb9f2d6134dc";

    public static BlueprintFeature DeftManeuvers;
    public static BlueprintFeature PowerfulManeuvers;

    public static void Apply() {
      Main.Log.Log("ManeuverConsolidation: applying #5/#6/#8/#9");

      var deftImproved = new[] {
          FeatureRefs.ImprovedTrip,
          FeatureRefs.ImprovedDisarm,
          FeatureRefs.ImprovedDirtyTrick,
      };
      var powerfulImproved = new[] {
          FeatureRefs.ImprovedBullRush,
          FeatureRefs.ImprovedOverrun,
          FeatureRefs.ImprovedSunder,
      };

      // Iantorno's blog specifies no prerequisites for Deft Maneuvers — the
      // earlier design-doc draft suggested Dex 13, but that's not in the source.
      DeftManeuvers = FeatureConfigurator.New(DeftManeuversName, DeftManeuversGuid,
              FeatureGroup.Feat, FeatureGroup.CombatFeat)
          .SetDisplayName(LocalizationTool.CreateString("EitR.DeftManeuvers.Name", "Deft Maneuvers"))
          .SetDescription(LocalizationTool.CreateString("EitR.DeftManeuvers.Desc",
              "You don't provoke an attack of opportunity when performing a trip, disarm, " +
              "or dirty trick combat maneuver, and you gain a +2 bonus on those checks. " +
              "Replaces Improved Trip, Improved Disarm, and Improved Dirty Trick."))
          .SetIsClassFeature()
          .AddFacts(new() {
              FeatureRefs.ImprovedTrip.ToString(),
              FeatureRefs.ImprovedDisarm.ToString(),
              FeatureRefs.ImprovedDirtyTrick.ToString(),
          })
          .AddToFeatureSelection(FeatureSelectionRefs.BasicFeatSelection.ToString())
          .AddToFeatureSelection(FeatureSelectionRefs.FighterFeatSelection.ToString())
          .AddToFeatureSelection(FeatureSelectionRefs.CombatTrick.ToString())
          .Configure();

      // No prereq per Iantorno's blog (same note as Deft Maneuvers).
      PowerfulManeuvers = FeatureConfigurator.New(PowerfulManeuversName, PowerfulManeuversGuid,
              FeatureGroup.Feat, FeatureGroup.CombatFeat)
          .SetDisplayName(LocalizationTool.CreateString("EitR.PowerfulManeuvers.Name", "Powerful Maneuvers"))
          .SetDescription(LocalizationTool.CreateString("EitR.PowerfulManeuvers.Desc",
              "You don't provoke an attack of opportunity when performing a bull rush, " +
              "overrun, or sunder combat maneuver, and you gain a +2 bonus on those checks. " +
              "Replaces Improved Bull Rush, Improved Overrun, and Improved Sunder."))
          .SetIsClassFeature()
          .AddFacts(new() {
              FeatureRefs.ImprovedBullRush.ToString(),
              FeatureRefs.ImprovedOverrun.ToString(),
              FeatureRefs.ImprovedSunder.ToString(),
          })
          .AddToFeatureSelection(FeatureSelectionRefs.BasicFeatSelection.ToString())
          .AddToFeatureSelection(FeatureSelectionRefs.FighterFeatSelection.ToString())
          .AddToFeatureSelection(FeatureSelectionRefs.CombatTrick.ToString())
          .Configure();

      // Redirect Greater-X prereqs to the new consolidated feat, BEFORE we
      // strip the Improved-X feats (which would otherwise nuke those prereqs).
      foreach (var imp in deftImproved) {
        Helpers.RedirectPrerequisite(imp.Reference.Get(), DeftManeuvers);
      }
      foreach (var imp in powerfulImproved) {
        Helpers.RedirectPrerequisite(imp.Reference.Get(), PowerfulManeuvers);
      }

      // Strip all Improved-X feats from selections (existing characters keep them).
      var bypassSet = Patches.PrerequisiteFeature_Check_Patch.BypassedPrereqs;
      var redirects = Patches.PrerequisiteFeature_Check_Patch.Redirects;

      foreach (var imp in deftImproved) {
        var feat = imp.Reference.Get();
        Helpers.RemoveFromAllSelections(feat);
        bypassSet.Add(feat.AssetGuid);
        redirects[feat.AssetGuid] = DeftManeuvers;
      }
      foreach (var imp in powerfulImproved) {
        var feat = imp.Reference.Get();
        Helpers.RemoveFromAllSelections(feat);
        bypassSet.Add(feat.AssetGuid);
        redirects[feat.AssetGuid] = PowerfulManeuvers;
      }

      Main.Log.Log("ManeuverConsolidation: created Deft + Powerful Maneuvers; redirected Greater-X prereqs; stripped Improved-X");
    }
  }
}
