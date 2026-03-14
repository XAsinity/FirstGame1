using UnityEngine;

/// <summary>
/// Routes player input to the active character's ability and attack system.
/// Hotkeys: Mouse Left = Primary Attack, 1-4 = Ability slots 0-3.
/// Safe: does nothing if no character is selected or if <see cref="PlayerManager.InputEnabled"/> is false.
/// Logs all key presses and outcomes to the Unity Console for easy debugging.
/// Attach this component to the same GameObject as <see cref="PlayerManager"/> (or any persistent object in the scene).
/// </summary>
[DisallowMultipleComponent]
public class AbilityInput : MonoBehaviour
{
    void Update()
    {
        if (PlayerManager.Instance == null)
        {
            // No manager in scene
            return;
        }

        // Inputs are disabled until PlayerManager enables them (a character is selected)
        if (!PlayerManager.Instance.InputEnabled) return;

        var player = PlayerManager.Instance.CurrentCharacter;
        if (player == null) return;

        // Primary attack (M1)
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[Input] M1 pressed -> PrimaryAttack()");
            player.PrimaryAttack();
        }

        // Abilities 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[Input] Alpha1 pressed -> Ability 0");
            TryUseAbility(0, player);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[Input] Alpha2 pressed -> Ability 1");
            TryUseAbility(1, player);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("[Input] Alpha3 pressed -> Ability 2");
            TryUseAbility(2, player);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("[Input] Alpha4 pressed -> Ability 3");
            TryUseAbility(3, player);
        }
    }

    private void TryUseAbility(int index, Character player)
    {
        if (player == null) return;
        if (player.abilities == null) return;
        if (index < 0 || index >= player.abilities.Count) return;

        var ability = player.abilities[index];
        if (ability == null)
        {
            Debug.Log($"[Input] Ability slot {index} is empty.");
            return;
        }

        if (!player.CanUseAbility(ability))
        {
            Debug.Log($"[Input] Cannot use ability '{ability.abilityName}' right now (cooldown/resource).");
            return;
        }

        Debug.Log($"[Input] Using ability '{ability.abilityName}'");
        player.UseAbility(ability);
    }
}