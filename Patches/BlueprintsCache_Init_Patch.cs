using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;

namespace EitRForWotr.Patches {
  [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
  internal static class BlueprintsCache_Init_Patch {
    private static bool _patched = false;

    static void Postfix() {
      if (_patched) return;
      _patched = true;
      try {
        Main.Log.Log("BlueprintsCache.Init Postfix — applying mutators");
        Mutators.FreeCombatOptions.Apply();        // #4, #7, #11
        Mutators.PointBlankPrereqStrip.Apply();    // #10
        // Future phases:
        //   Mutators.TwoWeaponConsolidation.Apply();   // TWF tweak
        //   Mutators.ManeuverConsolidation.Apply();    // #5, #6, #8, #9
        //   Mutators.FinesseWeaponRules.Apply();       // #2, #3
        //   Mutators.DodgeMobilityMerge.Apply();       // #12
        //   Mutators.WeaponFeatGrouping.Apply();       // #1
      } catch (System.Exception ex) {
        Main.Log.Error(ex.ToString());
      }
    }
  }
}
