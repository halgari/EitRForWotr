using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;

namespace EitRForWotr.Mutators {
  /// <summary>
  /// Conditional cousin of <c>ReplaceCombatManeuverStat</c>: only swaps the
  /// CMB stat when the replacement's bonus is at least as high as Strength's.
  /// Mirrors <c>AttackStatReplacement</c>'s "no downgrades" behavior so
  /// globally-granted Agile Maneuvers can't hurt high-Str builds.
  ///
  /// In its own file (and using only Kingmaker types — no BlueprintCore /
  /// Harmony / UMM) so it can compile against the headless harness's
  /// publicized Assembly-CSharp under the net10 TFM as well as net472.
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
