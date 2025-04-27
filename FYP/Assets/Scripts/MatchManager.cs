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

    // public int connectedClients;

    //quick test to see if we are actually connecting to the server
    //IEnumerator TestConnection()
    //{
    //    UnityWebRequest request = UnityWebRequest.Get("https://zendevfyp.click");
    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError("Connection error: " + request.error);
    //    }
    //    else
    //    {
    //        Debug.Log("Successfully connected to the server");
    //    }
    //}

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

        roomCodes = new NetworkList<Link>();
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

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                serverAdress.Value = transport.ConnectionData.Address;
            }

            foreach (var link in roomCodes)
            {
                Debug.Log($"Room Code: {link.roomCode}, IP: {link.serverIp}");
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
        if (IsServer && !begunMatch.Value)
        {
            //SceneLoader.Instance.LoadScene("Arena", LoadSceneMode.Additive);
            begunMatch.Value = true;
        }
    }

    // private const string baseUrl = "http://localhost:3000"; // local 
    // private const string baseUrl = "https://13.51.167.138:3000";  // ubuntu linux ec2 address
    private const string baseUrl = "https://zendevfyp.click:3000";


    public void CreateRoomOnServer(int roomCode, string serverIp)
    {
        Debug.Log($"Creating room with Room Code: {roomCode} and Server IP: {serverIp}");
        RoomData data = new RoomData { roomCode = roomCode, serverIp = serverIp };
        string json = JsonConvert.SerializeObject(data); // Replaced JsonUtility
        StartCoroutine(PostRequest($"{baseUrl}/create-room", json));
    }


    //public void GetServerIpFromRoomCode(int roomCode, Action<string> callback)
    //{
    //    StartCoroutine(GetRequest($"{baseUrl}/get-ip/{roomCode}", callback));
    //}

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

    //private IEnumerator GetRequest(string url, Action<string> callback)
    //{
    //    UnityWebRequest request = UnityWebRequest.Get(url);
    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError("Get Error: " + request.error);
    //        callback(null);
    //    }
    //    else
    //    {
    //        RoomData response = JsonConvert.DeserializeObject<RoomData>(request.downloadHandler.text); // ← Updated
    //        callback(response.serverIp);
    //    }
    //}

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

    [Serializable]
    public class RoomData
    {
        public int roomCode;
        public string serverIp;
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

    // Overload PostAuthRequest
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


   
}
