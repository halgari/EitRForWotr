using System.Runtime.CompilerServices;
using TUnit.Core;

// Tests share a process-wide Game.Instance + BlueprintsCache, plus the
// AgileManeuvers integration test mutates a stock blueprint. Sequential
// execution keeps that mutation from racing with other tests.
[assembly: NotInParallel]

namespace EitRForWotr.Tests;

/// <summary>
/// Initialize the headless engine BEFORE any test or helper method JITs.
///
/// Why a ModuleInitializer specifically (not [Before(Assembly)], not Host
/// init inside helpers): the .NET JIT loads ALL types a method references
/// before executing any of the method's instructions. So `Host.EnsureInitialized()`
/// as line 1 of a Harness helper is too late — JIT'ing the helper's body
/// triggers the Assembly-CSharp load before line 1 runs, and without the
/// load-context resolver installed, that load fails with FileNotFoundException.
/// (Verified empirically by probes A1/A2 in commit history — both helpers
/// fail at JIT despite Host-at-line-1, before any test code executes.)
///
/// ModuleInitializer is the earliest hook: it fires when the runtime loads
/// the test assembly's module, before any test method or helper is invoked.
/// Host installs its AssemblyLoadContext.Resolving handler here, and from
/// then on every JIT-time Assembly-CSharp load goes through it cleanly.
/// </summary>
internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() => Wotr.Headless.Host.EnsureInitialized();
}
