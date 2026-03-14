using UnityEngine;

// Attach to any active GameObject (Main Camera, PlayerManager).
// This bypasses PlayerManager and Character logic and tests raw input.
public class InputDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Debug.Log("[InputDebugger] Alpha1 pressed");
        if (Input.GetKeyDown(KeyCode.Alpha2)) Debug.Log("[InputDebugger] Alpha2 pressed");
        if (Input.GetKeyDown(KeyCode.Alpha3)) Debug.Log("[InputDebugger] Alpha3 pressed");
        if (Input.GetKeyDown(KeyCode.Alpha4)) Debug.Log("[InputDebugger] Alpha4 pressed");
        if (Input.GetMouseButtonDown(0)) Debug.Log("[InputDebugger] Mouse0 pressed");
    }
}