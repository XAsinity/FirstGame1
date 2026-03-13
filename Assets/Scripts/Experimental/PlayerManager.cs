using UnityEngine;
using System.Reflection;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Tooltip("All characters available to pick from")]
    public CharacterProfile[] characterProfiles;

    [Tooltip("Where the player object should be spawned")]
    public Transform spawnPoint;

    [Tooltip("If true, automatically select the first profile on Start (useful while no selection UI exists)")]
    public bool autoSelectFirstProfile = true;

    // If false, AbilityInput will ignore inputs until a character is selected via SelectCharacter(...)
    [HideInInspector] public bool InputEnabled = false;

    [HideInInspector] public Character CurrentCharacter;

    // Optional: assign your camera follow component (can be null)
    public MonoBehaviour cameraFollowComponent;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Optionally spawn the first profile automatically for now.
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

        // instantiate new
        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : Vector3.zero;
        GameObject go = Instantiate(profile.characterPrefab, spawnPos, Quaternion.identity);
        Character ch = go.GetComponent<Character>();
        if (ch == null)
        {
            Debug.LogError("Selected prefab missing Character component.");
            Destroy(go);
            return;
        }

        // initialize runtime values from profile
        ch.InitializeFromProfile(profile);

        CurrentCharacter = ch;

        // enable inputs now that a character is picked and configured
        InputEnabled = true;

        // flexible camera hookup (try method, field, or fallback to SimpleCameraFollow)
        if (cameraFollowComponent != null)
        {
            MethodInfo setTargetMethod = cameraFollowComponent.GetType().GetMethod("SetTarget", new[] { typeof(Transform) });
            if (setTargetMethod != null)
            {
                setTargetMethod.Invoke(cameraFollowComponent, new object[] { go.transform });
            }
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
    }

    // Unselect the current character (disables inputs)
    public void UnselectCharacter()
    {
        if (CurrentCharacter != null)
            Destroy(CurrentCharacter.gameObject);
        CurrentCharacter = null;
        InputEnabled = false;
    }
}