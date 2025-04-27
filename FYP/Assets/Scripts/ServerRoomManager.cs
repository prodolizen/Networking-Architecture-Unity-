using UnityEngine;
using Unity.Netcode;

public class ServerRoomManager : MonoBehaviour
{
    public GameObject matchManagerPrefab;
    private bool spawned = false;
    public GameObject playerPrefab;

    private void Start()
    {
        //NetworkManager.Singleton.OnServerStarted += () =>
        //{
        //    if (!spawned)
        //    {
        //        var matchManager = Instantiate(matchManagerPrefab);
        //        matchManager.GetComponent<NetworkObject>().Spawn(true);
        //        spawned = true;
        //        Debug.Log("[ServerRoomManager] Spawned MatchManager at frame " + Time.frameCount);
        //    }
        //};

#if UNITY_SERVER
        Debug.Log("[SERVER] Headless build detected, starting server...");
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartServer();
        }
#endif

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void Update()
    {
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            if (!spawned)
            {
                var matchManager = Instantiate(matchManagerPrefab);
                matchManager.GetComponent<NetworkObject>().Spawn(true);
                spawned = true;
                Debug.Log("[ServerRoomManager] Spawned MatchManager at frame " + Time.frameCount);
            }
        };
    }

    void OnApplicationQuit()
    {
        NetworkManager.Singleton.Shutdown();
    }

    private void OnServerStarted()
    {
        Debug.Log("[SERVER] Server started, spawning MatchManager.");

        if (matchManagerPrefab != null)
        {
            var matchManager = Instantiate(matchManagerPrefab);
            matchManager.GetComponent<NetworkObject>().Spawn(true);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[SERVER] Client {clientId} connected, manually spawning player.");

        if (playerPrefab != null)
        {
            var playerInstance = Instantiate(playerPrefab);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
        else
        {
            Debug.LogError("No PlayerPrefab assigned to ServerRoomManager!");
        }
    }
}