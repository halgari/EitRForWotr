using System.Linq;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.FactLogic;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR TWF tweak — Improved TWF is merged into Greater TWF.
  ///
  /// Iantorno's spec: "prereqs Dex 17, TWF, BAB +6; standard extra off-hand
  /// attack plus a second at –5; at BAB +11, a third at –10."
  ///
  /// Implementation: GTWF's prereqs are relaxed (Dex 19→17, BAB +11→+6,
  /// ITWF→TWF); GTWF additionally grants the ITWF feature itself as a fact
  /// so any game logic that BAB-gates the third attack on "HasFeature(GTWF)
  /// at BAB+11" still works as expected. ITWF is then yanked from every
  /// selection list (existing characters keep it; nobody new can take it).
  /// </summary>
  internal static class TwoWeaponConsolidation {
    public static void Apply() {
      var twf = FeatureRefs.TwoWeaponFighting.Reference.Get();
      var itwf = FeatureRefs.TwoWeaponFightingImproved.Reference.Get();
      var gtwf = FeatureRefs.TwoWeaponFightingGreater.Reference.Get();
      if (twf == null || itwf == null || gtwf == null) {
        Main.Log.Error("TwoWeaponConsolidation: one of TWF/ITWF/GTWF blueprints not found");
        return;
      }

      var itwfFactRef = itwf.ToReference<BlueprintUnitFactReference>();

      FeatureConfigurator.For(FeatureRefs.TwoWeaponFightingGreater)
          .EditComponents<PrerequisiteStatValue>(
              p => p.Value = 17, p => p.Stat == StatType.Dexterity)
          .EditComponents<PrerequisiteStatValue>(
              p => p.Value = 6, p => p.Stat == StatType.BaseAttackBonus)
          .EditComponents<AddFacts>(
              af => Helpers.AppendFact(af, itwfFactRef),
              af => !af.Facts.Any(r => r != null && r.AssetGuid == itwf.AssetGuid))
          .Configure();

      // ITWF→TWF prereq redirect (RedirectPrerequisite sweeps every blueprint;
      // here only GTWF has a PrerequisiteFeature pointing at ITWF).
      Helpers.RedirectPrerequisite(itwf, twf);

      if (!gtwf.ComponentsArray.OfType<AddFacts>()
              .Any(af => af.Facts.Any(r => r != null && r.AssetGuid == itwf.AssetGuid))) {
        Main.Log.Warning("TwoWeaponConsolidation: GTWF had no AddFacts to augment — third-attack scaling may not work");
      }

      // Pull ITWF from every selection list and strip its prereq references
      // (the only inbound was GTWF, which we already redirected to TWF).
      Helpers.RemoveFromAllSelections(itwf);

      Main.Log.Log("TwoWeaponConsolidation: GTWF prereqs relaxed (Dex 17, TWF, BAB +6); ITWF stripped from selections");
    }
  }
}
