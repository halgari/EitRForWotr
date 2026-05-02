using HarmonyLib;
using UnityModManagerNet;

namespace EitRForWotr {
  public static class Main {
    public static Harmony HarmonyInstance;
    public static UnityModManager.ModEntry.ModLogger Log;

    public static bool Load(UnityModManager.ModEntry modEntry) {
      Log = modEntry.Logger;
      Log.Log($"Loading version {modEntry.Info.Version}");
      HarmonyInstance = new Harmony(modEntry.Info.Id);
      HarmonyInstance.PatchAll(typeof(Main).Assembly);
      modEntry.OnToggle = OnToggle;
      Log.Log("Load complete");
      return true;
    }

    private static bool OnToggle(UnityModManager.ModEntry e, bool active) {
      // Disabling at runtime is a no-op: blueprint mutations applied at startup
      // can't be cleanly reverted without a game restart.
      return active;
    }
  }
}
