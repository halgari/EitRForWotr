using System.Collections.Generic;
using System.Reflection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;

namespace EitRForWotr.Components {
  /// <summary>
  /// Cosmetic fix for vanilla <c>RuleCalculateBaseCMB.OnTrigger</c>: the
  /// rule honours <c>ReplaceStrength</c> for the bonus VALUE (it reads
  /// from the replacement stat) but adds the modifier with
  /// <c>Stat = StatType.Strength</c> hardcoded — so a Dex-on-CMB swap
  /// renders as "Strength: +N" in the combat log. This walks the rule's
  /// modifier list post-trigger and rewrites the offending modifier to
  /// reflect the actual stat used.
  ///
  /// Pure label change — does not affect the numeric result, and is a no-op
  /// when ReplaceStrength isn't set.
  /// </summary>
  public static class CmbLabelRewriter {
    static readonly FieldInfo BonusField =
        typeof(RulebookEvent).GetField("m_ModifiableBonus",
            BindingFlags.Instance | BindingFlags.NonPublic);

    static readonly FieldInfo ModifiersField =
        typeof(ModifiableBonus).GetField("m_Modifiers",
            BindingFlags.Instance | BindingFlags.NonPublic);

    public static void RewriteForReplacement(RuleCalculateBaseCMB rule) {
      if (rule == null || !rule.ReplaceStrength.HasValue) return;
      var replacement = rule.ReplaceStrength.Value;
      if (replacement == StatType.Strength) return;

      var bonus = BonusField?.GetValue(rule) as ModifiableBonus;
      if (bonus == null) return;
      var mods = ModifiersField?.GetValue(bonus) as List<Modifier>;
      if (mods == null) return;

      for (int i = 0; i < mods.Count; i++) {
        var m = mods[i];
        if (m.Stat != StatType.Strength) continue;
        // The original modifier was added via the public Modifier(int, StatType)
        // ctor (in RuleCalculateBaseCMB.OnTrigger), which leaves Fact/Type/
        // Descriptor at their defaults. Reconstructing with the same ctor
        // and the replacement stat preserves all other fields.
        mods[i] = new Modifier(m.Value, replacement);
        return;
      }
    }
  }
}
