using System.Linq;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// EitR changes #2 + #3 + #10 — globally-granted, no-prereq base feats.
  ///
  /// • Weapon Finesse: Dex-on-attack with Finessable weapons (#2)
  /// • Agile Maneuvers: Dex-on-CMB with Finessable weapons (#3)
  /// • Point-Blank Shot: +1 to-hit / +1 damage on ranged within 30 ft (#10)
  ///
  /// Implementation: WOTR's stock blueprints for these three feats already
  /// implement the mechanical effects. We just grant them as facts globally
  /// and strip them from selections (no Harmony patch needed).
  ///
  /// Note on PBS: the blog says "Gone. Precise Shot replaces it as a
  /// prerequisite…" — silent on what happens to the +1/+1 effect. Reading
  /// the post in context, every other "Gone" feat preserves its effect (auto-
  /// grant, merge, or consolidation), and PBS is described as a feat-tax of
  /// the same kind. Treating it as auto-granted matches that pattern.
  ///
  /// Note on Finessable weapons: the blog's only non-light example is the
  /// rapier — already tagged Finessable in stock WOTR's
  /// `WeaponCategoryExtension.Data` table, along with estoc, elven curved
  /// blade, starknife, dueling sword, and sawtooth sabre. No weapon
  /// re-tagging is required for blog faithfulness.
  /// </summary>
  internal static class FinesseWeaponRules {
    private const string FeatureName = "EitRFinesseRules";
    private const string FeatureGuid = "6d44b1a7e8c049cba0ac4f2628b1b9ce";

    public static BlueprintFeature EitrFinesse;

    public static void Apply() {
      Main.Log.Log("FinesseWeaponRules: applying #2/#3/#10 (no-prereq base feats)");

      EitrFinesse = FeatureConfigurator.New(FeatureName, FeatureGuid, FeatureGroup.Feat)
          .SetDisplayName(LocalizationTool.CreateString("EitR.Finesse.Name", "Base Combat Feats (EitR)"))
          .SetDescription(LocalizationTool.CreateString("EitR.Finesse.Desc",
              "Weapon Finesse, Agile Maneuvers, and Point-Blank Shot are granted " +
              "automatically — no feat slot required."))
          .SetIsClassFeature()
          .AddFacts(new() {
              FeatureRefs.WeaponFinesse.ToString(),
              FeatureRefs.AgileManeuvers.ToString(),
              FeatureRefs.PointBlankShot.ToString(),
          })
          .Configure();

      // Inject into level 1 of BasicFeatsProgression (every PC + most NPCs).
      var basicProg = ProgressionRefs.BasicFeatsProgression.Reference.Get();
      if (basicProg?.LevelEntries != null && basicProg.LevelEntries.Length > 0) {
        var lvl1 = basicProg.LevelEntries[0];
        lvl1.SetFeatures(lvl1.Features.Append(EitrFinesse));
        Main.Log.Log("FinesseWeaponRules: appended EitRFinesseRules to BasicFeatsProgression L1");
      }

      // Strip Weapon Finesse + Agile Maneuvers from every selection list and
      // any prereq chain (Agile Maneuvers happens to require Weapon Finesse —
      // both go).
      var wf = FeatureRefs.WeaponFinesse.Reference.Get();
      var am = FeatureRefs.AgileManeuvers.Reference.Get();
      Helpers.RemoveFromAllSelections(wf);
      Helpers.RemoveFromAllSelections(am);

      // Stock ReplaceCombatManeuverStat is unconditional — it always swaps
      // Str → Dex, which is a downgrade for high-Str builds (Animal Growth,
      // Legendary Proportions, large pets with Fury's Fall). Replace it with
      // a Dex≥Str conditional, mirroring AttackStatReplacement (Weapon Finesse).
      FeatureConfigurator.For(FeatureRefs.AgileManeuvers)
          .RemoveComponents(c => c is ReplaceCombatManeuverStat)
          .AddComponent(new ConditionalReplaceCombatManeuverStat { StatType = StatType.Dexterity })
          .Configure();

      var bypassSet = Patches.PrerequisiteFeature_Check_Patch.BypassedPrereqs;
      bypassSet.Add(wf.AssetGuid);
      bypassSet.Add(am.AssetGuid);

      Main.Log.Log("FinesseWeaponRules: stripped Weapon Finesse + Agile Maneuvers from all selection lists");
    }
  }

  /// <summary>
  /// Conditional cousin of <see cref="ReplaceCombatManeuverStat"/>: only
  /// swaps the CMB stat when the replacement's bonus is at least as high as
  /// Strength's. Mirrors <c>AttackStatReplacement</c>'s "no downgrades"
  /// behavior so globally-granted Agile Maneuvers can't hurt high-Str builds.
  /// </summary>
  [AllowedOn(typeof(BlueprintUnitFact), false)]
  public class ConditionalReplaceCombatManeuverStat : UnitFactComponentDelegate,
      IInitiatorRulebookHandler<RuleCalculateBaseCMB>,
      IRulebookHandler<RuleCalculateBaseCMB>,
      ISubscriber,
      IInitiatorRulebookSubscriber {
    public StatType StatType;

    public void OnEventAboutToTrigger(RuleCalculateBaseCMB evt) {
      var str = Owner.Stats.Strength;
      var replacement = Owner.Stats.GetStat(StatType) as ModifiableValueAttributeStat;
      if (str != null && replacement != null && replacement.Bonus >= str.Bonus) {
        evt.ReplaceStrength = StatType;
      }
    }

    public void OnEventDidTrigger(RuleCalculateBaseCMB evt) { }
  }
}
