using System.Linq;
using System.Reflection;
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
    private static readonly FieldInfo PrereqFeatureField =
        typeof(PrerequisiteFeature).GetField("m_Feature",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo AddFactsArrayField =
        typeof(AddFacts).GetField("m_Facts",
            BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Apply() {
      var twf = Helpers.Get<BlueprintFeature>(FeatureRefs.TwoWeaponFighting.ToString());
      var itwf = Helpers.Get<BlueprintFeature>(FeatureRefs.TwoWeaponFightingImproved.ToString());
      var gtwf = Helpers.Get<BlueprintFeature>(FeatureRefs.TwoWeaponFightingGreater.ToString());
      if (twf == null || itwf == null || gtwf == null) {
        Main.Log.Error("TwoWeaponConsolidation: one of TWF/ITWF/GTWF blueprints not found");
        return;
      }

      var twfRef = twf.ToReference<BlueprintFeatureReference>();
      var itwfFactRef = itwf.ToReference<BlueprintUnitFactReference>();
      bool addedItwfToAddFacts = false;

      foreach (var c in gtwf.ComponentsArray) {
        switch (c) {
          case PrerequisiteStatValue stat when stat.Stat == StatType.Dexterity:
            stat.Value = 17;
            break;
          case PrerequisiteStatValue stat when stat.Stat == StatType.BaseAttackBonus:
            stat.Value = 6;
            break;
          case PrerequisiteFeature pr when pr.Feature == itwf:
            PrereqFeatureField.SetValue(pr, twfRef);
            break;
          case AddFacts addFacts: {
            var existing = (BlueprintUnitFactReference[])AddFactsArrayField.GetValue(addFacts);
            if (existing != null && !existing.Any(r => r != null && r.Guid == itwf.AssetGuid)) {
              var augmented = existing.Concat(new[] { itwfFactRef }).ToArray();
              AddFactsArrayField.SetValue(addFacts, augmented);
              addedItwfToAddFacts = true;
            }
            break;
          }
        }
      }

      if (!addedItwfToAddFacts) {
        Main.Log.Warning("TwoWeaponConsolidation: GTWF had no AddFacts to augment — third-attack scaling may not work");
      }

      // Pull ITWF from every selection list and strip its prereq references
      // (the only inbound was GTWF, which we already redirected to TWF).
      Helpers.RemoveFromAllSelections(itwf);

      Main.Log.Log("TwoWeaponConsolidation: GTWF prereqs relaxed (Dex 17, TWF, BAB +6); ITWF stripped from selections");
    }
  }
}
