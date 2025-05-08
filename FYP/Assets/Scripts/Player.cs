using Unity.Netcode;
using UnityEngine;
using System;

public class Player : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>();
    [SerializeField] private int maxHealth = 100;
    public event Action<int, int> OnHealthChanged;
    private Player otherPlayer;
    public NetworkVariable<bool> IsWinner = new NetworkVariable<bool>(false);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Health.Value = maxHealth;

        Health.OnValueChanged += (oldH, newH) =>
        {
            OnHealthChanged?.Invoke(oldH, newH);
            if (newH <= 0 && IsServer)
                HandleDeath();
        };
    }

    /// <summary>
    /// Called *on the server* to apply damage.
    /// Clients should never call this directly.
    /// </summary>
    public void ApplyDamage(int amount)
    {
        Debug.Log($"[Server] Applying {amount} damage to {OwnerClientId}");
        Health.Value = Mathf.Max(Health.Value - amount, 0);
    }

    private void HandleDeath()
    {
        Debug.Log($"Player {OwnerClientId} died!");

        otherPlayer.Score.Value++;

        gameObject.GetComponent<PlayerMovement>().ResetSpawnPos();
        otherPlayer.GetComponent<PlayerMovement>().ResetSpawnPos();

        StartCoroutine(DelayedHealthReset());

        if (MatchManager.Instance != null && otherPlayer.Score.Value >= 5)
        {
            otherPlayer.IsWinner.Value = true;
            MatchManager.Instance.EndMatch();
        }
    }

    //public void DealDamage(int damage)
    //{
    //    Debug.Log("dealing damage");
    //    otherPlayer.ApplyDamage(damage); 
    //}

    [ServerRpc(RequireOwnership = false)]
    public void DealDamageServerRpc(int damageAmount)
    {
        if (otherPlayer != null)
        {
            otherPlayer.ApplyDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning("no other player");
        }
    }

    private void Update()
    {
        if (otherPlayer == null)
        {
            Player[] players = GameObject.FindObjectsOfType<Player>();

            foreach (Player p in players)
            {
                if (p != this)
                    otherPlayer = p;
            }
        }

    }

    private System.Collections.IEnumerator DelayedHealthReset()
    {
        yield return new WaitForSeconds(0.1f); // short delay to ensure state sync

        Health.Value = maxHealth;
        otherPlayer.Health.Value = maxHealth;

        Debug.Log($"Health reset: P{OwnerClientId}={Health.Value}, P{otherPlayer.OwnerClientId}={otherPlayer.Health.Value}");
    }

}