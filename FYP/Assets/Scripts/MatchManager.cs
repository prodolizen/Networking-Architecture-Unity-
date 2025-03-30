using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    // Default overrides allow all clients to read, but only the server can write
    public NetworkVariable<int> score = new NetworkVariable<int>(0);
    public NetworkVariable<bool> matchActive = new NetworkVariable<bool>(false);

    public NetworkVariable<bool> hostReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> clientReady = new NetworkVariable<bool>(false);

    public int connectedClients;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (IsServer)
        {
            connectedClients = NetworkManager.Singleton.ConnectedClients.Count;

            // Only start the match when both players are connected AND ready
            if (connectedClients == 2 && hostReady.Value && clientReady.Value)
            {
                matchActive.Value = true;
                StartMatch();
            }
            else
            {
                matchActive.Value = false;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyStateServerRpc(bool isReady, ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            hostReady.Value = isReady;
        }
        else
        {
            clientReady.Value = isReady;
        }

        Debug.Log($"Client {clientId} ready status: {isReady}");
    }

    private void StartMatch()
    {
        if (IsServer)
        {
            Debug.Log("Both players are ready. Starting match...");
            SceneLoader.Instance.LoadScene("Arena", LoadSceneMode.Additive);
        }
    }

    //manual player spawning, removed as I was able to get stuff working even with the server automatically spawning players
    //private void SpawnPlayer(ulong clientId)
    //{
    //    NetworkObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>();

    //    if (playerPrefab == null)
    //    {
    //        Debug.LogError("Player Prefab is missing a NetworkObject component!");
    //        return;
    //    }

    //    // Instantiate and Spawn the player for the given client
    //    NetworkObject playerInstance = Instantiate(playerPrefab);
    //    playerInstance.SpawnAsPlayerObject(clientId);
    //}

}
