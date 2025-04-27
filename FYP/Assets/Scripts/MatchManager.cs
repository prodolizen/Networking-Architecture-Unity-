using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json; // ← Added

//[Serializable]
//public class AccountData
//{
//    public string username;
//    public string password;

//    public AccountData(string user, string pass)
//    {
//        username = user;
//        password = pass;
//    }
//}

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    public NetworkVariable<int> score = new NetworkVariable<int>(0);
    public NetworkVariable<bool> matchActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hostReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> clientReady = new NetworkVariable<bool>(false);
    public NetworkVariable<FixedString64Bytes> serverAdress = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<bool> begunMatch = new NetworkVariable<bool>(false);
    public NetworkVariable<int> connectedClients = new NetworkVariable<int>(0);

    public bool devOverride = false;

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

        //roomCodes = new NetworkList<Link>();
    }

    void Update()
    {
        if (IsServer)
        {
            connectedClients.Value = NetworkManager.Singleton.ConnectedClients.Count;

            if (connectedClients.Value == 2 && hostReady.Value && clientReady.Value || devOverride == true)
            {
                matchActive.Value = true;
                StartMatch();
            }
            else
            {
                matchActive.Value = false;
            }
            Debug.Log("gellogelo");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                serverAdress.Value = transport.ConnectionData.Address;
            }

            //foreach (var link in roomCodes)
            //{
            //    Debug.Log($"Room Code: {link.roomCode}, IP: {link.serverIp}");
            //}
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
        if (IsServer && !begunMatch.Value)
        {
            //SceneLoader.Instance.LoadScene("Arena", LoadSceneMode.Additive);
            begunMatch.Value = true;
        }
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[MatchManager] NetworkSpawned and Instance set ");
        }
        else
        {
            Debug.LogWarning("[MatchManager] NetworkSpawned but instance already set!");
        }
    }




}