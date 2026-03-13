using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Tooltip("All characters available to pick from")]
    public CharacterProfile[] characterProfiles;

    [Tooltip("Where the player object should be spawned")]
    public Transform spawnPoint;

    [HideInInspector] public Character CurrentCharacter;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this to select a character by index (e.g., UI button)
    public void SelectCharacter(int index)
    {
        if (index < 0 || index >= characterProfiles.Length) return;
        SelectCharacter(characterProfiles[index]);
    }

    // Call this to select via profile directly
    public void SelectCharacter(CharacterProfile profile)
    {
        if (profile == null || profile.characterPrefab == null) return;

        // Destroy previous player
        if (CurrentCharacter != null)
            Destroy(CurrentCharacter.gameObject);

        // Instantiate new
        GameObject go = Instantiate(profile.characterPrefab, spawnPoint != null ? spawnPoint.position : Vector3.zero, Quaternion.identity);
        Character ch = go.GetComponent<Character>();
        if (ch == null)
        {
            Debug.LogError("Selected prefab missing Character component.");
            return;
        }

        // Initialize runtime character from profile (this will copy stats and abilities)
        ch.InitializeFromProfile(profile);

        CurrentCharacter = ch;

        // Optional: tell your camera to follow the player here
        var cam = Camera.main;
        if (cam != null)
        {
            // Example: if you have a follow script, set its target
            var follow = cam.GetComponent<SimpleCameraFollow>();
            if (follow != null) follow.target = go.transform;
        }
    }
}