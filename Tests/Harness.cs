using System.Reflection;
using EitRForWotr.Mutators;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Wotr.Headless;

namespace EitRForWotr.Tests;

/// <summary>
/// Test-side helpers. Every public surface here uses only stdlib +
/// Wotr.Headless types — NO Assembly-CSharp types in signatures, return
/// values, or default-parameter values. This matters because test method
/// bodies that reference Assembly-CSharp types directly fail at JIT time
/// (the runtime tries to load Assembly-CSharp before any test code runs,
/// so before <c>Host.EnsureInitialized()</c> can install the resolver).
/// Routing everything through this class moves those references into
/// methods whose bodies JIT lazily — after Host's resolver is in place.
/// </summary>
internal static class Harness
{
    /// <summary>
    /// Create a synthetic BlueprintFeature carrying a single
    /// <c>ConditionalReplaceCombatManeuverStat</c> whose replacement stat
    /// is <paramref name="statName"/>, register it under
    /// <paramref name="guidHex"/>, and return the GUID for
    /// <c>Unit.WithFeat(...)</c>.
    /// </summary>
    public static string RegisterConditionalCmbStatFeature(string guidHex, string statName)
    {
        Host.EnsureInitialized();
        var statType = (StatType)Enum.Parse(typeof(StatType), statName);
        var component = new ConditionalReplaceCombatManeuverStat { StatType = statType };
        return RegisterFeatureWithComponent(guidHex, "EitRTest_Conditional", component);
    }

    /// <summary>
    /// Test-side equivalent of <c>FinesseWeaponRules.Apply</c>'s
    /// AgileManeuvers patch: drop the stock unconditional
    /// <c>ReplaceCombatManeuverStat</c>, add our Dex≥Str conditional.
    /// Idempotent (safe to call repeatedly within a process).
    /// </summary>
    public static void PatchAgileManeuvers(string guidHex)
    {
        Host.EnsureInitialized();
        var bpGuid = new BlueprintGuid(Guid.Parse(guidHex));
        var bp = ResourcesLibrary.BlueprintsCache.Load(bpGuid) as BlueprintScriptableObject
            ?? throw new InvalidOperationException("AgileManeuvers blueprint not loaded");

        var current = bp.ComponentsArray ?? Array.Empty<BlueprintComponent>();
        if (current.Any(c => c is ConditionalReplaceCombatManeuverStat)) return;

        var filtered = current.Where(c => c is not ReplaceCombatManeuverStat).ToList();
        filtered.Add(new ConditionalReplaceCombatManeuverStat { StatType = StatType.Dexterity });

        var componentsField = WalkUpForField(typeof(BlueprintScriptableObject), "Components");
        componentsField.SetValue(bp, filtered.ToArray());
    }

    /// <summary>
    /// Drive Owlcat's <c>RuleCalculateBaseCMB</c> on <paramref name="unit"/>.
    /// Returns the final bonus, the name of the replacement stat (if any),
    /// and the StatType label the combat log would show for the ability-
    /// derived modifier — all as primitives so the caller doesn't need to
    /// touch Assembly-CSharp.
    ///
    /// Also runs <c>CmbLabelRewriter</c> after the engine to mirror what
    /// our Harmony postfix does in prod (Tests/ has no UMM/Harmony stack;
    /// invoking the rewriter manually keeps test behavior in lockstep
    /// with the live mod).
    /// </summary>
    public static CmbResult TriggerCmb(Unit unit)
    {
        Host.EnsureInitialized();
        var entity = (UnitEntityData)unit.Entity;
        var rule = new RuleCalculateBaseCMB(entity);
        Rulebook.Trigger(rule);
        EitRForWotr.Components.CmbLabelRewriter.RewriteForReplacement(rule);

        // Find the ability-derived modifier in the rule's bonus list. CMB
        // only ever puts one ability stat in there (the Str-or-replacement
        // line); other modifiers carry BaseAttackBonus / AdditionalCMB /
        // AdditionalAttackBonus / Size etc.
        string? abilityLabel = null;
        var bonus = ReadBonus(rule);
        if (bonus != null)
        {
            foreach (var m in bonus)
            {
                if (m.Stat == StatType.Strength
                    || m.Stat == StatType.Dexterity
                    || m.Stat == StatType.Constitution
                    || m.Stat == StatType.Intelligence
                    || m.Stat == StatType.Wisdom
                    || m.Stat == StatType.Charisma)
                {
                    abilityLabel = m.Stat.ToString();
                    break;
                }
            }
        }
        return new CmbResult(rule.Result, rule.ReplaceStrength?.ToString(), abilityLabel);
    }

    static readonly FieldInfo BonusField =
        typeof(Kingmaker.RuleSystem.RulebookEvent).GetField("m_ModifiableBonus",
            BindingFlags.Instance | BindingFlags.NonPublic);

    static IEnumerable<Kingmaker.RuleSystem.Rules.Modifier>? ReadBonus(RuleCalculateBaseCMB rule) =>
        (BonusField?.GetValue(rule) as Kingmaker.RuleSystem.Rules.ModifiableBonus)?.Modifiers;

    /// <summary>
    /// Diagnostic: dump an attribute stat's full breakdown — base value,
    /// modified value, bonus, and every modifier with its descriptor and
    /// source fact. Used to investigate why a particular stat is higher
    /// or lower than expected.
    /// </summary>
    public static string DumpStat(Unit unit, string statName)
    {
        Host.EnsureInitialized();
        var stats = ((UnitEntityData)unit.Entity).Descriptor.Stats;
        var stat = (Kingmaker.EntitySystem.Stats.ModifiableValue)
            stats.GetType()
                .GetField(statName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(stats)!;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{statName}: Base={stat.BaseValue} Modified={stat.ModifiedValue}");
        foreach (var mod in stat.Modifiers)
        {
            var srcName = (mod.Source as Kingmaker.EntitySystem.EntityFact)?.Blueprint?.GetType().Name
                ?? mod.Source?.GetType().Name ?? "<no source>";
            var srcBp = (mod.Source as Kingmaker.EntitySystem.EntityFact)?.Blueprint?.ToString() ?? "";
            sb.AppendLine($"  +{mod.ModValue} [{mod.ModDescriptor}] from {srcName} {srcBp}");
        }
        return sb.ToString().TrimEnd();
    }

    static string RegisterFeatureWithComponent(string guidHex, string name, BlueprintComponent component)
    {
        var bpGuid = new BlueprintGuid(Guid.Parse(guidHex));
        if (ResourcesLibrary.BlueprintsCache.Load(bpGuid) != null) return guidHex;

        var feature = (BlueprintFeature)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(BlueprintFeature));

        typeof(BlueprintComponent)
            .GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            !.SetValue(component, name + "_Component");

        WalkUpForField(typeof(BlueprintFeature), "Components")
            .SetValue(feature, new[] { component });
        WalkUpForField(typeof(BlueprintFeature), "AssetGuid")
            .SetValue(feature, bpGuid);
        // m_Name lives on UnityEngine.Object (past BlueprintScriptableObject);
        // not required to make the blueprint functional, just nice-to-have for
        // diagnostics. OK if it's missing.
        WalkUpForFieldOrNull(typeof(BlueprintFeature), "m_Name")
            ?.SetValue(feature, name);

        ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(bpGuid, feature);
        return guidHex;
    }

    static FieldInfo WalkUpForField(Type startType, string name) =>
        WalkUpForFieldOrNull(startType, name)
        ?? throw new InvalidOperationException($"field {name} not found walking up from {startType.Name}");

    static FieldInfo? WalkUpForFieldOrNull(Type startType, string name)
    {
        for (var t = startType; t != null; t = t.BaseType)
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f;
        }
        return null;
    }
}

/// <summary>
/// Result of <see cref="Harness.TriggerCmb(Unit)"/>. <see cref="ReplacedWith"/>
/// is the string name of the swap target stat (e.g. <c>"Dexterity"</c>) or
/// <c>null</c> if no swap component fired.
/// </summary>
public readonly record struct CmbResult(int Bonus, string? ReplacedWith, string? AbilityModLabel);
