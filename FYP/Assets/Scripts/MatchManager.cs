using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    public static event Action OnMatchManagerReady; // <-- new event

    public NetworkVariable<int> score = new NetworkVariable<int>(0);
    public NetworkVariable<bool> matchActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hostReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> clientReady = new NetworkVariable<bool>(false);
    public NetworkVariable<FixedString64Bytes> serverAdress = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<bool> begunMatch = new NetworkVariable<bool>(false);
    public NetworkVariable<int> connectedClients = new NetworkVariable<int>(0);

    public bool devOverride = false;

    private Dictionary<ulong, bool> clientReadyStates = new Dictionary<ulong, bool>();

    void Update()
    {
        if (IsServer)
        {
            connectedClients.Value = NetworkManager.Singleton.ConnectedClients.Count;

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                serverAdress.Value = transport.ConnectionData.Address;
            }
        }
    }

    private void Start()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void SetReadyStateServerRpc(bool isReady, ulong clientId)
    {
        Debug.Log($"[ServerRpc] Client {clientId} set ready to {isReady}");

        if (clientReadyStates.ContainsKey(clientId))
        {
            clientReadyStates[clientId] = isReady;
        }
        else
        {
            clientReadyStates.Add(clientId, isReady);
        }

        Debug.Log($"Ready states now: {string.Join(", ", clientReadyStates.Select(kvp => $"Client {kvp.Key}: {kvp.Value}"))}");

        // Check if all players are ready
        if (clientReadyStates.Count == NetworkManager.Singleton.ConnectedClients.Count &&
            clientReadyStates.All(kvp => kvp.Value == true))
        {
            Debug.Log("[MatchManager] All players ready. Starting match!");
            matchActive.Value = true;
            StartMatch();
        }
    }


    private void StartMatch()
    {
        if (IsServer && !begunMatch.Value)
        {
            begunMatch.Value = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[MatchManager] OnNetworkSpawn — isServer={IsServer}, Instance assigned");

        OnMatchManagerReady?.Invoke(); // <-- Notify listeners
    }

    public void testing()
    {
        Debug.Log("[Testing] MatchManager method called");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientReadyStates.ContainsKey(clientId))
        {
            clientReadyStates.Remove(clientId);
        }
    }

}
