using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The twin prefab carries two identical, enabled TiagoDualArmRosSync components (same
/// RosConnector, same model, same IK). Both home the arms, both call switch_controller and
/// both publish every command, and changing `mode` in the Inspector only changes one of
/// them — the other keeps driving Gazebo.
///
/// After the scene loads (before any Update), this keeps the first component per model and
/// disables the others. The prefab is left untouched.
/// </summary>
public static class TwinSingleArmSync
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DisableDuplicates()
    {
        var seen = new HashSet<Object>();
        var syncs = Object.FindObjectsByType<TiagoDualArmRosSync>(FindObjectsSortMode.InstanceID);
        foreach (TiagoDualArmRosSync sync in syncs)
        {
            if (!sync.enabled) continue;
            Object key = sync.model != null ? (Object)sync.model : sync.gameObject;
            if (seen.Add(key)) continue;

            sync.enabled = false;
            Debug.Log($"[TwinSingleArmSync] disabled duplicate TiagoDualArmRosSync on " +
                      $"'{sync.gameObject.name}' (same model as another one).");
        }
    }
}
