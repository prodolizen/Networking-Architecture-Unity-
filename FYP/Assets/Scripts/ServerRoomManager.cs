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
using Newtonsoft.Json;

[Serializable]
public class AccountData
{
    public string username;
    public string password;

    public AccountData(string user, string pass)
    {
        username = user;
        password = pass;
    }
}

[Serializable]
public class RoomData
{
    public int roomCode;
    public string serverIp;
}

public class ServerRoomManager : NetworkBehaviour
{
    public struct Link : INetworkSerializable, IEquatable<Link>
    {
        public int roomCode;
        public FixedString64Bytes serverIp;

        public bool Equals(Link other)
        {
            return roomCode == other.roomCode && serverIp.Equals(other.serverIp);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref roomCode);
            serializer.SerializeValue(ref serverIp);
        }
    }

    [SerializeField]
    public NetworkList<Link> roomCodes;

    public GameObject matchManagerPrefab;
    private bool spawned = false;
    public GameObject playerPrefab;

    public static ServerRoomManager Instance;

    private const string baseUrl = "https://zendevfyp.click:3000";

    private bool hadClientConnected = false;
    private string dedicatedServerName = "unity-server-7777";

    private void Awake()
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
        roomCodes = new NetworkList<Link>();
    }

    private void Start()
    {
#if UNITY_SERVER
        if (Application.isBatchMode)
        {
            Debug.Log("[SERVER] Headless build detected, starting server...");
            if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.StartServer();
                SceneManager.LoadScene("Arena", LoadSceneMode.Additive);
            }
        }
#endif

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnServerStarted()
    {
        Debug.Log("[SERVER] Server started.");

        if (!spawned && matchManagerPrefab != null)
        {
            Debug.Log("[SERVER] Spawning MatchManager prefab...");
            var matchManager = Instantiate(matchManagerPrefab);
            matchManager.GetComponent<NetworkObject>().Spawn(true);
            spawned = true;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void Update()
    {
        // ❌ No spawning MatchManager in Update anymore — removed

#if UNITY_SERVER
        if (Application.isBatchMode)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
#endif

    }

    private void OnApplicationQuit()
    {
        NetworkManager.Singleton.Shutdown();
    }

    private void OnClientConnected(ulong clientId)
    {
        hadClientConnected = true;
        Debug.Log($"[SERVER] Client {clientId} connected, manually spawning player.");

        if (playerPrefab == null)
        {
            Debug.LogError("No PlayerPrefab assigned to ServerRoomManager!");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            if (networkClient.PlayerObject != null)
            {
                Debug.LogWarning(
                    $"Client {clientId} already has a PlayerObject " +
                    $"(NetworkObjectId = {networkClient.PlayerObject.NetworkObjectId}), skipping spawn."
                );
                return;
            }
        }

        var playerInstance = Instantiate(playerPrefab);
        var netObj = playerInstance.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
    }

    public void CreateRoomOnServer(int roomCode, string serverIp)
    {
        Debug.Log($"Creating room with Room Code: {roomCode} and Server IP: {serverIp}");
        RoomData data = new RoomData { roomCode = roomCode, serverIp = serverIp };
        string json = JsonConvert.SerializeObject(data);
        StartCoroutine(PostRequest($"{baseUrl}/create-room", json));
    }

    private IEnumerator PostRequest(string url, string json)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Post Error: " + request.error);
        }
        else
        {
            Debug.Log("Room created: " + request.downloadHandler.text);
        }
    }

    public void GetServerIpFromRoomCode(int roomCode, Action<string> callback)
    {
        Debug.Log($"{baseUrl}/get-ip/{roomCode}");
        StartCoroutine(GetRequest($"{baseUrl}/get-ip/{roomCode}", callback));
    }

    private IEnumerator GetRequest(string url, Action<string> callback)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Get Error: " + request.error);
            callback(null);
        }
        else
        {
            Debug.Log("success request");
            RoomData response = JsonConvert.DeserializeObject<RoomData>(request.downloadHandler.text);
            callback(response.serverIp);
        }
    }

    public void DeleteRoomFromServer(int roomCode)
    {
        StartCoroutine(DeleteRequest($"{baseUrl}/delete-room/{roomCode}"));
    }

    private IEnumerator DeleteRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Delete(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Delete Error: " + request.error);
        }
        else
        {
            Debug.Log("Room deleted: " + request.downloadHandler.text);
        }
    }

    public void LoginAccount(string username, string password, Action onSuccess = null)
    {
        AccountData data = new AccountData(username, password);
        string json = JsonUtility.ToJson(data);
        StartCoroutine(PostAuthRequest($"{baseUrl}/login", json, "Logged In", onSuccess));
    }

    public void RegisterAccount(string username, string password, Action onSuccess = null)
    {
        AccountData data = new AccountData(username, password);
        string json = JsonUtility.ToJson(data);
        StartCoroutine(PostAuthRequest($"{baseUrl}/register", json, "Registered", onSuccess));
    }

    private IEnumerator PostAuthRequest(string url, string json, string successMsg, Action onSuccess = null)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Auth Error: {request.responseCode} - {request.downloadHandler.text}");
        }
        else
        {
            Debug.Log(successMsg + ": " + request.downloadHandler.text);
            onSuccess?.Invoke();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ServerRoomManager] NetworkSpawned and Instance set");
        }
        else
        {
            Debug.LogWarning("[ServerRoomManager] NetworkSpawned but instance already set!");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[SERVER] Client {clientId} disconnected — cleaning up.");

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            var playerObj = networkClient.PlayerObject;
            if (playerObj != null)
            {
                playerObj.Despawn(true);
            }
        }

        if (hadClientConnected && NetworkManager.Singleton.ConnectedClients.Count == 0)
        {
            Debug.Log("[SERVER] No remaining clients. Shutting down this dedicated server...");
            StartCoroutine(ShutdownDedicatedServer());
        }
    }

    private IEnumerator ShutdownDedicatedServer()
    {
        string url = "https://zendevfyp.click:3000/stop-dedicated-server";
        var body = JsonConvert.SerializeObject(new { name = dedicatedServerName });

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("[SERVER] Failed to shut down: " + request.error);
        else
            Debug.Log("[SERVER] Successfully shut down server: " + request.downloadHandler.text);
    }

}
