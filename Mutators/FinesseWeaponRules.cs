using System.Linq;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR changes #2 + #3 — Weapon Finesse and Agile Maneuvers go away as
  /// feats; everyone gets Dex-on-attack and Dex-on-CMB when wielding a weapon
  /// tagged Finessable.
  ///
  /// Implementation: WOTR's Weapon Finesse and Agile Maneuvers blueprints
  /// already key off `WeaponSubCategory.Finessable` (via `AttackStatReplacement`
  /// and `ReplaceCombatManeuverStat` components respectively). So we just grant
  /// both blueprints as facts globally and strip them from selections —
  /// no Harmony patch on `RuleCalculateCMB` needed.
  ///
  /// Known v0.1 limitation: this only covers weapons WOTR already tagged with
  /// the Finessable subcategory (most light weapons). Iantorno's spec extends
  /// the tag to rapier/whip/spiked-chain/elven-curve-blade/estoc/starknife;
  /// adding those tags is deferred — needs a sweep over BlueprintItemWeapon.
  /// </summary>
  internal static class FinesseWeaponRules {
    private const string FeatureName = "EitRFinesseRules";
    private const string FeatureGuid = "6d44b1a7e8c049cba0ac4f2628b1b9ce";

    public static BlueprintFeature EitrFinesse;

    public static void Apply() {
      Main.Log.Log("FinesseWeaponRules: applying #2/#3");

      EitrFinesse = FeatureConfigurator.New(FeatureName, FeatureGuid, FeatureGroup.Feat)
          .SetDisplayName(Helpers.CreateString("EitR.Finesse.Name", "Finesse Rules (EitR)"))
          .SetDescription(Helpers.CreateString("EitR.Finesse.Desc",
              "When wielding a finessable weapon, you may use Dexterity instead of " +
              "Strength on attack rolls and combat maneuver checks — granted automatically, " +
              "no feat slot required."))
          .SetIsClassFeature()
          .AddFacts(new() {
              FeatureRefs.WeaponFinesse.ToString(),
              FeatureRefs.AgileManeuvers.ToString(),
          })
          .Configure();

      // Inject into level 1 of BasicFeatsProgression (every PC + most NPCs).
      var basicProg = Helpers.Get<BlueprintProgression>(ProgressionRefs.BasicFeatsProgression.ToString());
      if (basicProg?.LevelEntries != null && basicProg.LevelEntries.Length > 0) {
        var lvl1 = basicProg.LevelEntries[0];
        lvl1.SetFeatures(lvl1.Features.Append(EitrFinesse));
        Main.Log.Log("FinesseWeaponRules: appended EitRFinesseRules to BasicFeatsProgression L1");
      }

      // Strip Weapon Finesse + Agile Maneuvers from every selection list and
      // any prereq chain (Agile Maneuvers happens to require Weapon Finesse —
      // both go).
      Helpers.RemoveFromAllSelections(Helpers.Get<BlueprintFeature>(FeatureRefs.WeaponFinesse.ToString()));
      Helpers.RemoveFromAllSelections(Helpers.Get<BlueprintFeature>(FeatureRefs.AgileManeuvers.ToString()));

      Main.Log.Log("FinesseWeaponRules: stripped Weapon Finesse + Agile Maneuvers from all selection lists");
    }
  }
}
