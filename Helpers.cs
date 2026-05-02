using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Localization;

namespace EitRForWotr {
  internal static class Helpers {
    public static T Get<T>(string guid) where T : BlueprintScriptableObject =>
        ResourcesLibrary.TryGetBlueprint<T>(BlueprintGuid.Parse(guid));

    public static IEnumerable<T> AllBlueprints<T>() where T : SimpleBlueprint {
      var result = new List<T>();
      ResourcesLibrary.BlueprintsCache.ForEachLoaded((_, bp) => {
        if (bp is T t) result.Add(t);
      });
      return result;
    }

    public static LocalizedString CreateString(string key, string value) {
      LocalizationManager.CurrentPack?.PutString(key, value);
      return new LocalizedString { Key = key };
    }

    private static readonly FieldInfo SelectionAllFeaturesField =
        typeof(BlueprintFeatureSelection).GetField("m_AllFeatures",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo SelectionFeaturesField =
        typeof(BlueprintFeatureSelection).GetField("m_Features",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo PrereqListFeaturesField =
        typeof(PrerequisiteFeaturesFromList).GetField("m_Features",
            BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Remove a feature from every BlueprintFeatureSelection, and clear any
    /// PrerequisiteFeature / PrerequisiteFeaturesFromList references to it.
    /// Doesn't delete the blueprint — keeps existing saves loadable.
    /// </summary>
    public static void RemoveFromAllSelections(BlueprintFeature feat) {
      if (feat == null) return;
      var guid = feat.AssetGuid;

      foreach (var sel in AllBlueprints<BlueprintFeatureSelection>()) {
        FilterRefArray(sel, SelectionAllFeaturesField, guid);
        FilterRefArray(sel, SelectionFeaturesField, guid);
      }

      foreach (var bp in AllBlueprints<BlueprintFeature>()) {
        var components = bp.ComponentsArray;
        if (components == null || components.Length == 0) continue;
        var keep = new List<BlueprintComponent>(components.Length);
        bool changed = false;
        foreach (var c in components) {
          if (c is PrerequisiteFeature pr && pr.Feature == feat) {
            changed = true;
            continue;
          }
          if (c is PrerequisiteFeaturesFromList prList) {
            var refs = (BlueprintFeatureReference[])PrereqListFeaturesField.GetValue(prList);
            if (refs != null && refs.Any(r => r != null && r.Guid == guid)) {
              var filtered = refs.Where(r => r == null || r.Guid != guid).ToArray();
              if (filtered.Length == 0) { changed = true; continue; }
              PrereqListFeaturesField.SetValue(prList, filtered);
            }
          }
          keep.Add(c);
        }
        if (changed) bp.ComponentsArray = keep.ToArray();
      }
    }

    private static void FilterRefArray(BlueprintFeatureSelection sel, FieldInfo field, BlueprintGuid guid) {
      var refs = (BlueprintFeatureReference[])field.GetValue(sel);
      if (refs == null || refs.Length == 0) return;
      var filtered = refs.Where(r => r == null || r.Guid != guid).ToArray();
      if (filtered.Length != refs.Length) field.SetValue(sel, filtered);
    }
  }
}
