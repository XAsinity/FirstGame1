using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class AbilityInput : MonoBehaviour
{
    void Update()
    {
        var player = PlayerManager.Instance;
        if (player == null || player.CurrentCharacter == null) return;
        var ch = player.CurrentCharacter;

        if (Input.GetKeyDown(KeyCode.Alpha1) && ch.abilities.Count > 0) ch.UseAbility(ch.abilities[0]);
        if (Input.GetKeyDown(KeyCode.Alpha2) && ch.abilities.Count > 1) ch.UseAbility(ch.abilities[1]);
        if (Input.GetKeyDown(KeyCode.Alpha3) && ch.abilities.Count > 2) ch.UseAbility(ch.abilities[2]);
        if (Input.GetKeyDown(KeyCode.Alpha4) && ch.abilities.Count > 3) ch.UseAbility(ch.abilities[3]);
    }
}