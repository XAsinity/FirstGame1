using UnityEngine;

// Attach to the PlayerManager GameObject to monitor selection/input state.
public class PlayerStateDebugger : MonoBehaviour
{
    void Update()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.Log("[PlayerStateDebugger] PlayerManager.Instance == null");
            return;
        }

        Debug.Log($"[PlayerStateDebugger] InputEnabled={PlayerManager.Instance.InputEnabled}, CurrentCharacter={(PlayerManager.Instance.CurrentCharacter != null ? PlayerManager.Instance.CurrentCharacter.name : "null")}");
        // run once per second:
        enabled = false;
        Invoke(nameof(Reenable), 1f);
    }

    void Reenable() { enabled = true; }
}