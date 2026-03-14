using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to the GameObject that has (or is parent of) the Animator you want to debug.
/// It prints diagnostic information on Awake and then optionally logs parameter/state changes and periodic dumps.
/// </summary>
[DisallowMultipleComponent]
public class AnimatorDebugger : MonoBehaviour
{
    [Tooltip("If left empty the script will find the first Animator on this GameObject or its children.")]
    public Animator animator;

    [Tooltip("Seconds between periodic status dumps (0 = disabled)")]
    public float periodicDumpInterval = 1.0f;

    [Tooltip("If true, prints parameter changes as they happen")]
    public bool logParameterChanges = true;

    [Tooltip("If true, prints state changes for each layer")]
    public bool logStateChanges = true;

    private float timer;
    private Dictionary<string, float> lastFloat = new Dictionary<string, float>();
    private Dictionary<string, int> lastInt = new Dictionary<string, int>();
    private Dictionary<string, bool> lastBool = new Dictionary<string, bool>();
    private List<int> lastStateHashPerLayer = new List<int>();

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        LogStartupInfo();
        CacheParameters();
        CacheLayerStates();
    }

    void Update()
    {
        if (animator == null) return;

        if (logParameterChanges)
            DetectParameterChanges();

        if (logStateChanges)
            DetectStateChanges();

        if (periodicDumpInterval > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= periodicDumpInterval)
            {
                timer = 0f;
                DumpStatus();
            }
        }
    }

    private void LogStartupInfo()
    {
        if (animator == null)
        {
            Debug.LogError("[AnimatorDebugger] No Animator found on this GameObject or its children.");
            return;
        }

        Debug.Log("[AnimatorDebugger] Animator found on: " + animator.gameObject.name);
        Debug.LogFormat("[AnimatorDebugger] enabled={0} isInitialized={1} hasBoundPlayables={2}",
            animator.enabled, animator.isInitialized, animator.hasBoundPlayables);
        Debug.LogFormat("[AnimatorDebugger] applyRootMotion={0} updateMode={1} cullingMode={2}",
            animator.applyRootMotion, animator.updateMode, animator.cullingMode);
        Debug.LogFormat("[AnimatorDebugger] runtimeController={0}",
            animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL");
        Debug.LogFormat("[AnimatorDebugger] avatar={0}",
            animator.avatar != null ? (animator.avatar.isValid ? animator.avatar.name + " (valid)" : animator.avatar.name + " (invalid)") : "NULL");

        if (animator.parameters == null || animator.parameters.Length == 0)
            Debug.LogWarning("[AnimatorDebugger] Animator has NO parameters defined. If your Animator transitions rely on parameters (e.g., 'Speed') they must be present in the controller.");
    }

    private void CacheParameters()
    {
        if (animator == null) return;
        lastFloat.Clear();
        lastInt.Clear();
        lastBool.Clear();

        foreach (var p in animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float:
                    lastFloat[p.name] = animator.GetFloat(p.name);
                    break;
                case AnimatorControllerParameterType.Int:
                    lastInt[p.name] = animator.GetInteger(p.name);
                    break;
                case AnimatorControllerParameterType.Bool:
                    lastBool[p.name] = animator.GetBool(p.name);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    // initialize as 0; triggers have no getter
                    lastInt[p.name] = 0;
                    break;
            }
            Debug.LogFormat("[AnimatorDebugger] Param: {0} (Type={1})", p.name, p.type);
        }
    }

    private void CacheLayerStates()
    {
        lastStateHashPerLayer.Clear();
        if (animator == null) return;
        int layers = animator.layerCount;
        for (int i = 0; i < layers; i++)
        {
            var si = animator.GetCurrentAnimatorStateInfo(i);
            lastStateHashPerLayer.Add(si.fullPathHash);
            Debug.LogFormat("[AnimatorDebugger] Layer {0} - stateHash={1} nameHash={2} normalizedTime={3:F2} looping={4}",
                i, si.fullPathHash, si.shortNameHash, si.normalizedTime, si.loop);
        }
    }

    private void DetectParameterChanges()
    {
        if (animator == null) return;

        foreach (var p in animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float:
                    float f = animator.GetFloat(p.name);
                    if (!lastFloat.ContainsKey(p.name) || Mathf.Abs(f - lastFloat[p.name]) > 0.01f)
                    {
                        Debug.LogFormat("[AnimatorDebugger] Param changed: {0} (Float) {1} -> {2}", p.name, lastFloat.GetValueOrDefault(p.name), f);
                        lastFloat[p.name] = f;
                    }
                    break;
                case AnimatorControllerParameterType.Int:
                    int iv = animator.GetInteger(p.name);
                    if (!lastInt.ContainsKey(p.name) || lastInt[p.name] != iv)
                    {
                        Debug.LogFormat("[AnimatorDebugger] Param changed: {0} (Int) {1} -> {2}", p.name, lastInt.GetValueOrDefault(p.name), iv);
                        lastInt[p.name] = iv;
                    }
                    break;
                case AnimatorControllerParameterType.Bool:
                    bool b = animator.GetBool(p.name);
                    if (!lastBool.ContainsKey(p.name) || lastBool[p.name] != b)
                    {
                        Debug.LogFormat("[AnimatorDebugger] Param changed: {0} (Bool) {1} -> {2}", p.name, lastBool.GetValueOrDefault(p.name), b);
                        lastBool[p.name] = b;
                    }
                    break;
                case AnimatorControllerParameterType.Trigger:
                    // Can't directly read trigger state; skip
                    break;
            }
        }
    }

    private void DetectStateChanges()
    {
        if (animator == null) return;
        int layers = animator.layerCount;
        for (int i = 0; i < layers; i++)
        {
            var si = animator.GetCurrentAnimatorStateInfo(i);
            int hash = si.fullPathHash;
            if (i < lastStateHashPerLayer.Count)
            {
                if (lastStateHashPerLayer[i] != hash)
                {
                    Debug.LogFormat("[AnimatorDebugger] Layer {0} state changed: oldHash={1} -> newHash={2} normalizedTime={3:F2}",
                        i, lastStateHashPerLayer[i], hash, si.normalizedTime);
                    lastStateHashPerLayer[i] = hash;
                }
            }
            else
            {
                lastStateHashPerLayer.Add(hash);
                Debug.LogFormat("[AnimatorDebugger] Layer {0} initial state: hash={1} normalizedTime={2:F2}", i, hash, si.normalizedTime);
            }
        }
    }

    /// <summary>
    /// Dumps a snapshot of animator status (parameters and layer states) to the Console.
    /// </summary>
    public void DumpStatus()
    {
        if (animator == null)
        {
            Debug.LogError("[AnimatorDebugger] DumpStatus called but Animator is null.");
            return;
        }

        Debug.Log("[AnimatorDebugger] --- STATUS DUMP ---");
        Debug.LogFormat("enabled={0}, isInitialized={1}, runtimeController={2}, avatar={3}",
            animator.enabled,
            animator.isInitialized,
            animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL",
            animator.avatar != null ? (animator.avatar.isValid ? animator.avatar.name + " (valid)" : animator.avatar.name + " (invalid)") : "NULL");

        Debug.LogFormat("applyRootMotion={0}, updateMode={1}, cullingMode={2}, layers={3}",
            animator.applyRootMotion, animator.updateMode, animator.cullingMode, animator.layerCount);

        // parameters
        foreach (var p in animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float:
                    Debug.LogFormat("Param Float: {0} = {1}", p.name, animator.GetFloat(p.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    Debug.LogFormat("Param Int: {0} = {1}", p.name, animator.GetInteger(p.name));
                    break;
                case AnimatorControllerParameterType.Bool:
                    Debug.LogFormat("Param Bool: {0} = {1}", p.name, animator.GetBool(p.name));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    Debug.LogFormat("Param Trigger: {0}", p.name);
                    break;
            }
        }

        // layers & state info
        for (int i = 0; i < animator.layerCount; i++)
        {
            var si = animator.GetCurrentAnimatorStateInfo(i);
            Debug.LogFormat("Layer {0}: stateHash={1}, shortNameHash={2}, normalizedTime={3:F2}, speed={4:F2}",
                i, si.fullPathHash, si.shortNameHash, si.normalizedTime, si.speed);
            var next = animator.GetNextAnimatorStateInfo(i);
            if (next.fullPathHash != 0)
                Debug.LogFormat(" Layer {0} nextStateHash={1} normalizedTime={2:F2}", i, next.fullPathHash, next.normalizedTime);
        }

        Debug.Log("[AnimatorDebugger] --- END STATUS DUMP ---");
    }
}

/// <summary>
/// Small helper extension for dictionary GetValueOrDefault.
/// </summary>
static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
        if (dict.TryGetValue(key, out var v)) return v;
        return default(TValue);
    }
}