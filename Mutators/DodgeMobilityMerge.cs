using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR change #12 — Mobility is merged into Dodge.
  ///
  /// Iantorno's spec: "Dodge (revised: +1 dodge AC, increasing to +4 vs. AoOs
  /// caused when you move out of or within a threatened tile)."
  ///
  /// WOTR's stock Mobility already gives `ACBonusAgainstAttacks` with
  /// `OnlyAttacksOfOpportunity: true, Value: 4` — close enough to the
  /// spec (WOTR doesn't differentiate movement-triggered AoOs from other AoOs).
  /// We just have Dodge AddFact Mobility, so taking Dodge gives the combined
  /// effect. Mobility itself gets stripped from selections; anything that
  /// previously required Mobility now requires Dodge.
  /// </summary>
  internal static class DodgeMobilityMerge {
    public static void Apply() {
      Main.Log.Log("DodgeMobilityMerge: applying #12");

      FeatureConfigurator.For(FeatureRefs.Dodge)
          .AddFacts(new() { FeatureRefs.Mobility.ToString() })
          .Configure();

      var mobility = Helpers.Get<BlueprintFeature>(FeatureRefs.Mobility.ToString());
      var dodge = Helpers.Get<BlueprintFeature>(FeatureRefs.Dodge.ToString());
      Helpers.RedirectPrerequisite(mobility, dodge);
      Helpers.RemoveFromAllSelections(mobility);

      Main.Log.Log("DodgeMobilityMerge: Dodge now grants Mobility; Mobility stripped from selections");
    }
  }
}
