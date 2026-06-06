using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using Wotr.Headless;

namespace EitRForWotr.Tests;

/// <summary>
/// Drive RuleCalculateBaseCMB through the real Owlcat engine to prove the
/// ConditionalReplaceCombatManeuverStat component (and the AgileManeuvers
/// blueprint patch that installs it) actually preserve high-Str CMB.
///
/// The first three tests attach the component to a synthetic feature on
/// the unit — fully isolated, don't touch shared blueprints. The fourth
/// runs a before/after pair against the *real* stock AgileManeuvers
/// blueprint to prove the bug + fix end-to-end.
///
/// Assertions go through string/int primitives only. Direct references
/// to Assembly-CSharp types in test method bodies fail at JIT time —
/// see AssemblyInfo.cs for the JIT-timing analysis.
/// </summary>
public class CmbTests
{
    // Synthetic feature GUIDs — any random GUIDs that don't collide with
    // stock blueprints. One per component-test variant so the synthetic
    // feature's stored StatType doesn't leak between tests.
    const string COND_FEAT_STR_HIGHER  = "11111111111111111111111111111101";
    const string COND_FEAT_DEX_HIGHER  = "11111111111111111111111111111102";
    const string COND_FEAT_EQUAL_STATS = "11111111111111111111111111111103";

    // Stock AgileManeuvers blueprint (from BlueprintCore's FeatureRefs).
    const string AGILE_MANEUVERS_GUID = "197306972c98bb843af738dc7529a7ac";

    [Test]
    public async Task Component_does_not_swap_when_Str_is_higher()
    {
        var feat = Harness.RegisterConditionalCmbStatFeature(COND_FEAT_STR_HIGHER, "Dexterity");
        var arena = new RulesArena(seed: 42);

        var unit = arena.SpawnFighter("strBuild", str: 20, dex: 10).WithBAB(5).WithFeat(feat);
        var control = arena.SpawnFighter("strBuild_ctl", str: 20, dex: 10).WithBAB(5);

        var cmb = Harness.TriggerCmb(unit);
        var controlCmb = Harness.TriggerCmb(control);

        await Assert.That(cmb.ReplacedWith).IsNull();
        await Assert.That(cmb.Bonus).IsEqualTo(controlCmb.Bonus);
    }

    [Test]
    public async Task Component_swaps_to_Dex_when_Dex_is_higher()
    {
        var feat = Harness.RegisterConditionalCmbStatFeature(COND_FEAT_DEX_HIGHER, "Dexterity");
        var arena = new RulesArena(seed: 42);

        var swapped = arena.SpawnFighter("dexBuild", str: 10, dex: 20).WithBAB(5).WithFeat(feat);
        var control = arena.SpawnFighter("dexBuild_ctl", str: 10, dex: 20).WithBAB(5);

        var cmb = Harness.TriggerCmb(swapped);
        var controlCmb = Harness.TriggerCmb(control);

        await Assert.That(cmb.ReplacedWith).IsEqualTo("Dexterity");
        // Dex(+5) replacing Str(+0) → swap gain = 5.
        await Assert.That(cmb.Bonus - controlCmb.Bonus).IsEqualTo(5);
    }

    [Test]
    public async Task Component_swaps_to_Dex_when_stats_are_equal()
    {
        var feat = Harness.RegisterConditionalCmbStatFeature(COND_FEAT_EQUAL_STATS, "Dexterity");
        var arena = new RulesArena(seed: 42);

        var unit = arena.SpawnFighter("balanced", str: 14, dex: 14).WithBAB(5).WithFeat(feat);
        var control = arena.SpawnFighter("balanced_ctl", str: 14, dex: 14).WithBAB(5);

        var cmb = Harness.TriggerCmb(unit);
        var controlCmb = Harness.TriggerCmb(control);

        // The >= boundary: equal bonuses still trip the swap. Numeric result
        // is identical either way; ReplaceStrength is the proof it fired.
        await Assert.That(cmb.ReplacedWith).IsEqualTo("Dexterity");
        await Assert.That(cmb.Bonus).IsEqualTo(controlCmb.Bonus);
    }

    /// <summary>
    /// Vanilla RuleCalculateBaseCMB adds the ability-mod modifier with
    /// Stat=Strength hardcoded even when ReplaceStrength is set, mis-
    /// labelling the combat log entry as "Strength" during a Dex swap.
    /// Our CmbLabelRewriter postfix rewrites the modifier so the log
    /// reflects the actual stat used. This test asserts both halves:
    /// no swap → Strength label; swap → Dexterity label.
    /// </summary>
    [Test]
    public async Task CmbLog_label_reflects_the_stat_actually_used()
    {
        var dexSwapFeat = Harness.RegisterConditionalCmbStatFeature(COND_FEAT_DEX_HIGHER, "Dexterity");
        var arena = new RulesArena(seed: 42);

        // No swap (control): Str is higher, the conditional component
        // declines to swap, log should say "Strength".
        var strUnit = arena.SpawnFighter("strLog", str: 20, dex: 10)
            .WithBAB(5).WithFeat(dexSwapFeat);
        var strResult = Harness.TriggerCmb(strUnit);
        await Assert.That(strResult.ReplacedWith).IsNull();
        await Assert.That(strResult.AbilityModLabel).IsEqualTo("Strength");

        // Swap: Dex is higher, the component swaps, our postfix rewrites
        // the modifier so the log shows "Dexterity" (was the bug — vanilla
        // still showed "Strength" with the Dex value).
        var dexUnit = arena.SpawnFighter("dexLog", str: 10, dex: 20)
            .WithBAB(5).WithFeat(dexSwapFeat);
        var dexResult = Harness.TriggerCmb(dexUnit);
        await Assert.That(dexResult.ReplacedWith).IsEqualTo("Dexterity");
        await Assert.That(dexResult.AbilityModLabel).IsEqualTo("Dexterity");
    }

    /// <summary>
    /// End-to-end against stock AgileManeuvers. First half is the regression
    /// baseline — stock blueprint downgrades Str on a high-Str build
    /// (the bug). Second half applies our blueprint patch and proves the
    /// same setup now keeps Str CMB.
    ///
    /// Combined into one test method so the order-dependent mutation stays
    /// in lockstep with the assertions that depend on it.
    /// </summary>
    [Test]
    public async Task Patched_AgileManeuvers_preserves_Str_CMB_on_high_Str_unit()
    {
        var arena = new RulesArena(seed: 42);

        // --- Baseline: stock AgileManeuvers downgrades Str → Dex ---
        var baseline = arena.SpawnFighter("baseline", str: 20, dex: 10)
            .WithBAB(5).WithFeat(AGILE_MANEUVERS_GUID);
        var baselineCmb = Harness.TriggerCmb(baseline);

        await Assert.That(baselineCmb.ReplacedWith).IsEqualTo("Dexterity");

        // --- Apply the fix ---
        Harness.PatchAgileManeuvers(AGILE_MANEUVERS_GUID);

        var patched = arena.SpawnFighter("patched", str: 20, dex: 10)
            .WithBAB(5).WithFeat(AGILE_MANEUVERS_GUID);
        var patchedCmb = Harness.TriggerCmb(patched);

        await Assert.That(patchedCmb.ReplacedWith).IsNull();
        // Str(+5) preserved instead of Dex(+0) — patched is 5 higher.
        await Assert.That(patchedCmb.Bonus - baselineCmb.Bonus).IsEqualTo(5);
    }
}
