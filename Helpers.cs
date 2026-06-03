using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic.FactLogic;

namespace EitRForWotr {
  /// <summary>
  /// Thin wrappers around things BlueprintCore doesn't already cover. Anything
  /// BlueprintCore exposes (blueprint lookup via Refs.X.Reference.Get(),
  /// LocalizationTool.CreateString, FeatureSelectionConfigurator,
  /// RemoveComponents, etc.) is used directly at call sites.
  /// </summary>
  internal static class Helpers {
    public static IEnumerable<T> AllBlueprints<T>() where T : SimpleBlueprint {
      var result = new List<T>();
      ResourcesLibrary.BlueprintsCache.ForEachLoaded((_, bp) => {
        if (bp is T t) result.Add(t);
      });
      return result;
    }

    // Private game fields with no public setter. BlueprintCore configurators
    // cover m_AllFeatures via RemoveFromAllFeatures but don't expose
    // m_Features, PrerequisiteFeature.m_Feature, PrerequisiteFeaturesFromList.m_Features,
    // or AddFacts.m_Facts. These FieldInfos are the canonical reflection bridges.
    private static readonly FieldInfo SelectionAllFeaturesField =
        typeof(BlueprintFeatureSelection).GetField("m_AllFeatures",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo SelectionFeaturesField =
        typeof(BlueprintFeatureSelection).GetField("m_Features",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo PrereqFeatureField =
        typeof(PrerequisiteFeature).GetField("m_Feature",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo PrereqListFeaturesField =
        typeof(PrerequisiteFeaturesFromList).GetField("m_Features",
            BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo AddFactsArrayField =
        typeof(AddFacts).GetField("m_Facts",
            BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Remove a feature from every BlueprintFeatureSelection (so it can no
    /// longer be picked at level-up) AND clear any prerequisite references
    /// to it. Doesn't delete the blueprint — keeps existing saves loadable.
    /// </summary>
    public static void RemoveFromAllSelections(BlueprintFeature feat) {
      if (feat == null) return;
      var guid = feat.AssetGuid;
      foreach (var sel in AllBlueprints<BlueprintFeatureSelection>()) {
        var allRefs = (BlueprintFeatureReference[])SelectionAllFeaturesField.GetValue(sel);
        bool inAll = allRefs?.Any(r => r != null && r.Guid == guid) ?? false;

        var featRefs = (BlueprintFeatureReference[])SelectionFeaturesField.GetValue(sel);
        bool inFeats = featRefs?.Any(r => r != null && r.Guid == guid) ?? false;

        if (inAll) {
          FeatureSelectionConfigurator.For(sel.ToReference<BlueprintFeatureSelectionReference>())
              .RemoveFromAllFeatures(r => r != null && r.Guid == guid)
              .Configure();
        }
        if (inFeats) {
          var filtered = featRefs.Where(r => r == null || r.Guid != guid).ToArray();
          SelectionFeaturesField.SetValue(sel, filtered);
        }
      }
      StripAsPrerequisite(feat);
    }

    /// <summary>
    /// Remove all PrerequisiteFeature / PrerequisiteFeaturesFromList
    /// references to <paramref name="feat"/> across every loaded feature.
    /// Leaves the feature itself selectable (use this for changes like
    /// "Point-Blank Shot is no longer a prereq for Precise Shot" where PBS
    /// itself stays in the feat list).
    /// </summary>
    public static void StripAsPrerequisite(BlueprintFeature feat) {
      if (feat == null) return;
      var guid = feat.AssetGuid;
      foreach (var bp in AllBlueprints<BlueprintFeature>()) {
        var components = bp.ComponentsArray;
        if (components == null || components.Length == 0) continue;
        bool touched = false;
        foreach (var c in components) {
          if (c is PrerequisiteFeature pr && pr.Feature == feat) { touched = true; break; }
          if (c is PrerequisiteFeaturesFromList prList && ListHasFeature(prList, guid)) {
            touched = true; break;
          }
        }
        if (!touched) continue;

        FeatureConfigurator.For(bp.ToReference<BlueprintFeatureReference>())
            .RemoveComponents(c =>
                (c is PrerequisiteFeature pr && pr.Feature == feat)
                || (c is PrerequisiteFeaturesFromList prList
                    && ListIsAllFeature(prList, guid)))
            .EditComponents<PrerequisiteFeaturesFromList>(
                prList => {
                  var refs = (BlueprintFeatureReference[])PrereqListFeaturesField.GetValue(prList);
                  var filtered = refs.Where(r => r == null || r.Guid != guid).ToArray();
                  PrereqListFeaturesField.SetValue(prList, filtered);
                },
                prList => ListHasFeature(prList, guid) && !ListIsAllFeature(prList, guid))
            .Configure();
      }
    }

    private static bool ListHasFeature(PrerequisiteFeaturesFromList prList, BlueprintGuid guid) {
      var refs = (BlueprintFeatureReference[])PrereqListFeaturesField.GetValue(prList);
      return refs != null && refs.Any(r => r != null && r.Guid == guid);
    }

    private static bool ListIsAllFeature(PrerequisiteFeaturesFromList prList, BlueprintGuid guid) {
      var refs = (BlueprintFeatureReference[])PrereqListFeaturesField.GetValue(prList);
      return refs != null
             && refs.Any(r => r != null && r.Guid == guid)
             && refs.All(r => r == null || r.Guid == guid);
    }

    /// <summary>
    /// For every blueprint, replace any PrerequisiteFeature pointing at
    /// <paramref name="oldFeat"/> with one pointing at <paramref name="newFeat"/>.
    /// Use this when consolidating chains (e.g. "Greater Trip now requires
    /// Deft Maneuvers instead of Improved Trip").
    /// </summary>
    public static void RedirectPrerequisite(BlueprintFeature oldFeat, BlueprintFeature newFeat) {
      if (oldFeat == null || newFeat == null) return;
      var newRef = newFeat.ToReference<BlueprintFeatureReference>();
      foreach (var bp in AllBlueprints<BlueprintFeature>()) {
        var components = bp.ComponentsArray;
        if (components == null || components.Length == 0) continue;
        if (!components.Any(c => c is PrerequisiteFeature pr && pr.Feature == oldFeat)) continue;
        FeatureConfigurator.For(bp.ToReference<BlueprintFeatureReference>())
            .EditComponents<PrerequisiteFeature>(
                pr => PrereqFeatureField.SetValue(pr, newRef),
                pr => pr.Feature == oldFeat)
            .Configure();
      }
    }

    /// <summary>
    /// Append a fact reference to an AddFacts component. Backing field
    /// AddFacts.m_Facts has no public setter in the game code.
    /// </summary>
    public static void AppendFact(AddFacts addFacts, BlueprintUnitFactReference factRef) {
      var existing = (BlueprintUnitFactReference[])AddFactsArrayField.GetValue(addFacts);
      var augmented = (existing ?? new BlueprintUnitFactReference[0]).Concat(new[] { factRef }).ToArray();
      AddFactsArrayField.SetValue(addFacts, augmented);
    }
  }
}
