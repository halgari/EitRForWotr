using System.Linq;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR changes #4, #7, #11 — Power Attack, Combat Expertise, and Deadly Aim
  /// become free combat options for any character with BAB ≥ +1.
  /// We keep the existing toggle activatables, strip the feat wrappers from
  /// every selection list, and grant the toggles globally via a new
  /// EitRFreeCombatOptions feature attached to BasicFeatsProgression.
  /// </summary>
  internal static class FreeCombatOptions {
    private const string FeatureName = "EitRFreeCombatOptions";
    private const string FeatureGuid = "184cd26c347d493bbe88c76a48129323";

    public static BlueprintFeature EitrFreebies;

    public static void Apply() {
      Main.Log.Log("FreeCombatOptions: applying #4/#7/#11");

      EitrFreebies = FeatureConfigurator.New(FeatureName, FeatureGuid, FeatureGroup.Feat)
          .SetDisplayName(Helpers.CreateString("EitR.FreeOptions.Name", "Combat Options (EitR)"))
          .SetDescription(Helpers.CreateString("EitR.FreeOptions.Desc",
              "Power Attack, Combat Expertise, and Deadly Aim are free combat options at " +
              "base attack bonus +1 — granted automatically, no feat slot required."))
          .SetIsClassFeature()
          .SetHideInUI(false)
          .AddPrerequisiteStatValue(StatType.BaseAttackBonus, value: 1)
          .AddFacts(new() {
              ActivatableAbilityRefs.PowerAttackToggleAbility.ToString(),
              ActivatableAbilityRefs.CombatExpertiseToggleAbility.ToString(),
              ActivatableAbilityRefs.DeadlyAimToggleAbility.ToString(),
          })
          .Configure();

      // Inject into level 1 of BasicFeatsProgression (every PC + most NPCs).
      var basicProg = Helpers.Get<BlueprintProgression>(ProgressionRefs.BasicFeatsProgression.ToString());
      if (basicProg?.LevelEntries != null && basicProg.LevelEntries.Length > 0) {
        var lvl1 = basicProg.LevelEntries[0];
        lvl1.SetFeatures(lvl1.Features.Append(EitrFreebies));
        Main.Log.Log("FreeCombatOptions: appended EitRFreeCombatOptions to BasicFeatsProgression L1");
      } else {
        Main.Log.Error("FreeCombatOptions: BasicFeatsProgression has no LevelEntries — skipping injection");
      }

      // Strip the feat wrappers from every selection list. PA/CE/DA can no
      // longer be taken as feats; their activatables come from EitrFreebies.
      Helpers.RemoveFromAllSelections(Helpers.Get<BlueprintFeature>(FeatureRefs.PowerAttackFeature.ToString()));
      Helpers.RemoveFromAllSelections(Helpers.Get<BlueprintFeature>(FeatureRefs.CombatExpertiseFeature.ToString()));
      Helpers.RemoveFromAllSelections(Helpers.Get<BlueprintFeature>(FeatureRefs.DeadlyAimFeature.ToString()));

      Main.Log.Log("FreeCombatOptions: stripped PA/CE/DA from all selection lists");
    }
  }
}
