using UnityEngine;
using System.Reflection;

/// <summary>
/// Spawns the selected character prefab (visual), parents it under a neutral VisualAnchor when needed,
/// initializes the Character, and assigns the spawned Animator instance to the PlayerController,
/// MovementAnimatorBridge, and Character if possible (so the capsule/controllers have a valid runtime Animator).
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Tooltip("All characters available to pick from")]
    public CharacterProfile[] characterProfiles;

    [Tooltip("Where the player object should be spawned / parented (e.g. PlayerCapsule or VisualAnchor)")]
    public Transform spawnPoint;

    [Tooltip("If true, automatically select the first profile on Start (useful while no selection UI exists)")]
    public bool autoSelectFirstProfile = true;

    [Header("Visual placement defaults (used if profile does not override)")]
    [Tooltip("Local offset applied to the spawned visual under the VisualAnchor")]
    public Vector3 visualLocalOffset = Vector3.zero;
    [Tooltip("Local rotation (Euler) applied to the spawned visual under the VisualAnchor")]
    public Vector3 visualLocalEulerOffset = Vector3.zero;
    [Tooltip("Default visual local scale applied if profile.visualScale is not set")]
    public Vector3 visualDefaultScale = Vector3.one;

    [HideInInspector] public bool InputEnabled = false;
    [HideInInspector] public Character CurrentCharacter;

    [Tooltip("Optional: camera follow component (assign Main Camera follow script here)")]
    public MonoBehaviour cameraFollowComponent;

    // Stored capsule values so they can be restored on UnselectCharacter
    private bool capsuleHadCharacterController = false;
    private float prevCapsuleHeight;
    private float prevCapsuleRadius;
    private Vector3 prevCapsuleCenter;

    private bool capsuleHadCollider = false;
    private float prevColliderHeight;
    private float prevColliderRadius;
    private Vector3 prevColliderCenter;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (autoSelectFirstProfile && characterProfiles != null && characterProfiles.Length > 0)
            SelectCharacter(characterProfiles[0]);
    }

    // Select by index (0-based)
    public void SelectCharacter(int index)
    {
        if (index < 0 || characterProfiles == null || index >= characterProfiles.Length) return;
        SelectCharacter(characterProfiles[index]);
    }

    // Select by profile
    public void SelectCharacter(CharacterProfile profile)
    {
        if (profile == null || profile.characterPrefab == null)
        {
            Debug.LogWarning("PlayerManager.SelectCharacter called with null profile or missing prefab.");
            return;
        }

        // destroy old
        if (CurrentCharacter != null)
            Destroy(CurrentCharacter.gameObject);

        // instantiate the visual prefab at spawn position/rotation (world)
        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;

        GameObject go = Instantiate(profile.characterPrefab, spawnPos, spawnRot);
        go.name = profile.characterPrefab.name;

        // Determine parent anchor: use an existing VisualAnchor under spawnPoint, create one if spawnPoint has non-1 scale,
        // otherwise parent directly to spawnPoint.
        Transform parentAnchor = null;
        if (spawnPoint != null)
        {
            parentAnchor = spawnPoint.Find("VisualAnchor");
            if (parentAnchor == null)
            {
                if (!ApproximatelyEqualVector3(spawnPoint.lossyScale, Vector3.one))
                {
                    GameObject anchorGO = new GameObject("VisualAnchor");
                    anchorGO.transform.SetParent(spawnPoint, false);
                    anchorGO.transform.localPosition = Vector3.zero;
                    anchorGO.transform.localRotation = Quaternion.identity;
                    anchorGO.transform.localScale = Vector3.one;
                    parentAnchor = anchorGO.transform;
                    Debug.LogWarning($"PlayerManager: spawnPoint had non-1 scale ({spawnPoint.lossyScale}). Created VisualAnchor under it to avoid scaling the visual.");
                }
                else
                {
                    parentAnchor = spawnPoint;
                }
            }
        }

        // Parent under anchor (or leave world-level if no spawnPoint)
        if (parentAnchor != null)
        {
            // Use SetParent(..., false) so local transform values correspond to the anchor
            go.transform.SetParent(parentAnchor, false);

            // Apply visual offsets from profile if present, otherwise fall back to defaults
            Vector3 localPos = (profile != null) ? profile.visualLocalOffset : visualLocalOffset;
            Vector3 localEuler = (profile != null) ? profile.visualLocalEulerOffset : visualLocalEulerOffset;
            Vector3 localScale = (profile != null && profile.visualScale != Vector3.zero) ? profile.visualScale : visualDefaultScale;

            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
        }
        else
        {
            // No spawnPoint: leave object in world at spawnPos/spawnRot and apply visual scale/rotation/position from profile
            go.transform.position = spawnPos;
            go.transform.rotation = spawnRot;

            if (profile != null && profile.visualScale != Vector3.zero)
                go.transform.localScale = profile.visualScale;
            else
                go.transform.localScale = visualDefaultScale;
        }

        // If the visual prefab contains physics/player-controller components and the capsule should own physics, remove them.
        var ccOnVisual = go.GetComponent<CharacterController>();
        if (ccOnVisual != null) Destroy(ccOnVisual);
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Always apply capsule sizing from the profile to ensure the physics capsule matches each character's proportions
        if (spawnPoint != null && profile != null)
        {
            var charCtrl = spawnPoint.GetComponent<CharacterController>();
            if (charCtrl != null)
            {
                capsuleHadCharacterController = true;
                prevCapsuleHeight = charCtrl.height;
                prevCapsuleRadius = charCtrl.radius;
                prevCapsuleCenter = charCtrl.center;

                charCtrl.height = profile.capsuleHeight;
                charCtrl.radius = profile.capsuleRadius;
                charCtrl.center = profile.capsuleCenter;
            }
            else
            {
                var cap = spawnPoint.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    capsuleHadCollider = true;
                    prevColliderHeight = cap.height;
                    prevColliderRadius = cap.radius;
                    prevColliderCenter = cap.center;

                    cap.height = profile.capsuleHeight;
                    cap.radius = profile.capsuleRadius;
                    cap.center = profile.capsuleCenter;
                }
            }
        }

        // Find Character component (root or children)
        Character ch = go.GetComponent<Character>() ?? go.GetComponentInChildren<Character>(true);
        if (ch == null)
        {
            Debug.LogError($"Selected prefab '{profile.characterPrefab.name}' missing Character component on root or children. Inspect the instantiated prefab hierarchy below.");
            DumpHierarchyWithComponents(go.transform);
            Destroy(go);
            return;
        }

        // Initialize runtime values from profile
        ch.InitializeFromProfile(profile);

        // Try to find Animator on the spawned visual
        Animator visualAnimator = go.GetComponentInChildren<Animator>(true);
        if (visualAnimator == null)
        {
            Debug.LogWarning($"PlayerManager: Spawned visual '{go.name}' but no Animator component was found on it.");
        }
        else
        {
            Debug.Log($"PlayerManager: Found Animator on spawned visual -> {visualAnimator.gameObject.name}");
        }

        // Assign the visual's animator to other runtime systems:
        //  - Character (if it has AssignAnimator method or 'animator' field)
        //  - PlayerController on spawnPoint (AssignAnimator method or 'animator' field)
        //  - MovementAnimatorBridge on spawnPoint (field or AssignAnimator)
        PlayerController pc = spawnPoint != null ? spawnPoint.GetComponent<PlayerController>() : null;
        MovementAnimatorBridge bridge = spawnPoint != null ? spawnPoint.GetComponent<MovementAnimatorBridge>() : null;

        if (visualAnimator != null)
        {
            // Character: prefer AssignAnimator method then try to set a field named 'animator'
            TryAssignAnimatorToComponent(ch, visualAnimator);

            if (pc != null)
                TryAssignAnimatorToComponent(pc, visualAnimator);

            if (bridge != null)
                TryAssignAnimatorToComponent(bridge, visualAnimator);
        }

        // Sync move speed from profile stats into PlayerController and MovementAnimatorBridge so
        // each character feels appropriately fast/slow without manually editing the capsule components.
        if (profile != null && profile.stats != null)
        {
            if (pc != null)
            {
                pc.MoveSpeed = profile.stats.moveSpeed;
                Debug.Log($"PlayerManager: Set PlayerController.MoveSpeed = {profile.stats.moveSpeed} from profile '{profile.displayName}'.");
            }

            if (bridge != null)
            {
                bridge.moveSpeed = profile.stats.moveSpeed;
                Debug.Log($"PlayerManager: Set MovementAnimatorBridge.moveSpeed = {profile.stats.moveSpeed} from profile '{profile.displayName}'.");
            }
        }

        CurrentCharacter = ch;
        InputEnabled = true;

        // Camera hookup (flexible)
        if (cameraFollowComponent != null)
        {
            MethodInfo setTargetMethod = cameraFollowComponent.GetType().GetMethod("SetTarget", new[] { typeof(Transform) });
            if (setTargetMethod != null)
                setTargetMethod.Invoke(cameraFollowComponent, new object[] { go.transform });
            else
            {
                var tField = cameraFollowComponent.GetType().GetField("target");
                if (tField != null && tField.FieldType == typeof(Transform))
                    tField.SetValue(cameraFollowComponent, go.transform);
            }
        }
        else
        {
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                var scf = mainCam.GetComponent<SimpleCameraFollow>();
                if (scf != null) scf.SetTarget(go.transform);
            }
        }

        Debug.Log($"PlayerManager: Spawned '{go.name}' and initialized character.");
    }

    /// <summary>
    /// Attempts multiple strategies to set the provided Animator instance onto the target component:
    /// 1) Invoke a method named 'AssignAnimator(Animator)'
    /// 2) Set a field named 'animator' (public or private)
    /// 3) Set a property named 'animator' or 'Anim' (public)
    /// 4) Invoke a method named 'SetAnimator' that takes an Animator
    /// Logs what it did (or warns if nothing matched).
    /// </summary>
    private void TryAssignAnimatorToComponent(object componentObj, Animator animator)
    {
        if (componentObj == null || animator == null) return;

        var comp = componentObj as Object;
        if (comp == null)
        {
            Debug.LogWarning("TryAssignAnimatorToComponent: componentObj is not a UnityEngine.Object");
            return;
        }

        var compType = componentObj.GetType();

        // 1) Try AssignAnimator(Animator)
        var assignMethod = compType.GetMethod("AssignAnimator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (assignMethod != null)
        {
            var ps = assignMethod.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == typeof(Animator))
            {
                assignMethod.Invoke(componentObj, new object[] { animator });
                Debug.Log($"PlayerManager: Called AssignAnimator on {compType.Name} -> {comp.name}");
                return;
            }
        }

        // 2) Try SetAnimator (another possible name)
        var setMethod = compType.GetMethod("SetAnimator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setMethod != null)
        {
            var ps = setMethod.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == typeof(Animator))
            {
                setMethod.Invoke(componentObj, new object[] { animator });
                Debug.Log($"PlayerManager: Called SetAnimator on {compType.Name} -> {comp.name}");
                return;
            }
        }

        // 3) Try to set a field named 'animator'
        var field = compType.GetField("animator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(Animator))
        {
            field.SetValue(componentObj, animator);
            Debug.Log($"PlayerManager: Set field 'animator' on {compType.Name} -> {comp.name}");
            return;
        }

        // 4) Try to set a property named 'animator' or 'Anim'
        var prop = compType.GetProperty("animator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(Animator) && prop.CanWrite)
        {
            prop.SetValue(componentObj, animator);
            Debug.Log($"PlayerManager: Set property 'animator' on {compType.Name} -> {comp.name}");
            return;
        }
        var prop2 = compType.GetProperty("Anim", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop2 != null && prop2.PropertyType == typeof(Animator) && prop2.CanWrite)
        {
            prop2.SetValue(componentObj, animator);
            Debug.Log($"PlayerManager: Set property 'Anim' on {compType.Name} -> {comp.name}");
            return;
        }

        Debug.LogWarning($"PlayerManager: Could not assign Animator to component {compType.Name} on GameObject {comp.name}. Consider adding an AssignAnimator(Animator) method or an 'animator' field/property.");
    }

    /// <summary>
    /// Destroys the current character and restores any capsule sizing we modified.
    /// </summary>
    public void UnselectCharacter()
    {
        if (CurrentCharacter != null)
            Destroy(CurrentCharacter.gameObject);

        // restore capsule values if we changed them
        if (spawnPoint != null)
        {
            if (capsuleHadCharacterController)
            {
                var charCtrl = spawnPoint.GetComponent<CharacterController>();
                if (charCtrl != null)
                {
                    charCtrl.height = prevCapsuleHeight;
                    charCtrl.radius = prevCapsuleRadius;
                    charCtrl.center = prevCapsuleCenter;
                }
                capsuleHadCharacterController = false;
            }
            if (capsuleHadCollider)
            {
                var cap = spawnPoint.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    cap.height = prevColliderHeight;
                    cap.radius = prevColliderRadius;
                    cap.center = prevColliderCenter;
                }
                capsuleHadCollider = false;
            }
        }

        CurrentCharacter = null;
        InputEnabled = false;
    }

    // Helper: recursive component-aware hierarchy dump (for debugging)
    private void DumpHierarchyWithComponents(Transform root, int depth = 0)
    {
        string pad = new string(' ', depth * 2);
        Debug.Log($"{pad}{root.name} - Components:", root.gameObject);

        var comps = root.GetComponents<Component>();
        if (comps == null || comps.Length == 0)
        {
            Debug.Log($"{pad}  (no components)", root.gameObject);
        }
        else
        {
            foreach (var c in comps)
            {
                if (c == null)
                    Debug.Log($"{pad}  MISSING SCRIPT", root.gameObject);
                else
                    Debug.Log($"{pad}  - {c.GetType().FullName}", root.gameObject);
            }
        }

        foreach (Transform child in root)
            DumpHierarchyWithComponents(child, depth + 1);
    }

    // Utility: robust vector approx equal for lossyScale checks
    private bool ApproximatelyEqualVector3(Vector3 a, Vector3 b, float tol = 0.0001f)
    {
        return Mathf.Abs(a.x - b.x) <= tol && Mathf.Abs(a.y - b.y) <= tol && Mathf.Abs(a.z - b.z) <= tol;
    }
}