using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Collections;
using UnityEngine.Networking;

[Serializable]
public class ServerInfo
{
    public string ip;
    public int port;
    public string name;
}

[Serializable]
public class ServerList
{
    public ServerInfo[] servers;
}

public class NetworkUI : NetworkBehaviour
{
    public GameObject uiComps;
    public GameObject layerOne;
    public GameObject layerTwo;
    public GameObject layerThree;
    public GameObject layerFour;
    public GameObject layerFive;
    public GameObject layerSix;
    public GameObject layerSeven;
    private GameObject activeLayer;
    private GameObject prevLayer;
    public GameObject score;

    private string gameIP;
    public GameObject crosshair;
    private Image crosshairImage;
    public TMP_Text clientCount;
    public TMP_Text matchID;
    public TMP_Text connecting;
    private bool isReady = false;
    public Button readyButton;
    public Image background;
    private string username;
    private string password;
    public TMP_Text loggedName;
    private bool loggedIn = false;
    public TMP_Dropdown serverList;
    private ServerInfo[] serverInfo;
    private bool firstRun;
    public GameObject matchManagerPrefab;
    private bool matchManagerReady = false;
    public TMP_Text result;
    public TMP_Text selfScore;
    public TMP_Text otherScore;

    private Player self;
    private Player other;

    private void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        MatchManager.OnMatchManagerReady += HandleMatchManagerReady; // <-- Added
    }

    private void OnDisable()
    {
        MatchManager.OnMatchManagerReady -= HandleMatchManagerReady; // <-- Added
    }

    void Start()
    {
        crosshairImage = crosshair.GetComponent<Image>();
        activeLayer = layerOne;

        //if (hostManager == null)
        //{
        //    hostManager = FindObjectOfType<HostManager>();
        //}
        firstRun = true;
        NetworkManager.Singleton.OnServerStarted += TrySpawnMatchManager;
    }

    void Update()
    {
        crosshairImage.color = Globals.CrosshairColour;

        if (MatchManager.Instance != null && matchManagerReady)
        {
            clientCount.text = "Connected Clients: " + MatchManager.Instance.connectedClients.Value;
            background.gameObject.SetActive(!MatchManager.Instance.matchActive.Value);

            if (MatchManager.Instance.matchActive.Value)
            {
                uiComps.SetActive(false);
                score.SetActive(true); 
            }
            crosshair.SetActive(MatchManager.Instance.matchActive.Value);

            if (self != null && other != null)
            {
                selfScore.text = self.Score.Value.ToString();
                otherScore.text = other.Score.Value.ToString(); 
            }
            
        }
        else
        {
            if (firstRun)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    if (FindObjectOfType<MatchManager>() == null)
                    {
                        var matchManager = Instantiate(matchManagerPrefab);
                        matchManager.GetComponent<NetworkObject>().Spawn(true);
                    }
                }
                firstRun = false;
            }
        }

        if (activeLayer == layerThree)
        {
            layerThree.SetActive(MatchManager.Instance != null);
            connecting.gameObject.SetActive(MatchManager.Instance == null);
        }

        if (MatchManager.Instance != null)
        {
            if (self == null || other == null)
            {
                var players = GameObject.FindObjectsOfType<Player>();
                foreach (var player in players)
                {
                    if (player.IsOwner)
                        self = player;
                    else
                        other = player;
                }
            }

            if (MatchManager.Instance.matchEnd.Value)
            {
                //var players = GameObject.FindObjectsOfType<Player>();
                //foreach (var player in players)
                //{
                //    if (player.IsOwner)
                //    {
                //        if (player.IsWinner.Value)
                //            result.text = "You Win!";
                //        else
                //            result.text = "You Lose!";
                //    }
                //}

                if (self.IsWinner.Value)
                    result.text = "You Win!";
                else
                    result.text = "You Lose!";

                SwapLayers(activeLayer, layerSeven);
                uiComps.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("A host is already running!");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.StartHost();

        if (NetworkManager.Singleton.IsServer)
        {
            SceneLoader.Instance.LoadScene("Arena", LoadSceneMode.Additive);
        }

        if (MatchManager.Instance != null)
        {
            int roomCode = UnityEngine.Random.Range(1000, 9999);
            string ip = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;
            MatchManager.Instance.serverAdress.Value = ip;

            Debug.Log("Setting server IP: " + ip);

            matchID.text = roomCode.ToString();
            ServerRoomManager.Instance.CreateRoomOnServer(roomCode, ip);
        }

        SwapLayers(layerOne, layerThree);
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected.");
    }

    private void SwapLayers(GameObject layer1, GameObject layer2)
    {
        layer1.SetActive(false);
        layer2.SetActive(true);
        activeLayer = layer2;
        prevLayer = layer1;
    }

    public void JoinGame()
    {
        SwapLayers(layerOne, layerTwo);
    }

    public void TakeIP(string s)
    {
        gameIP = s;
    }

    public void TakeUsername(string s)
    {
        username = s;
    }

    public void TakePassword(string s)
    {
        password = s;
    }

    public void Register()
    {
        if (MatchManager.Instance != null)
        {
            ServerRoomManager.Instance.RegisterAccount(username, password, OnAccountAuthenticated);
        }
    }

    public void Login()
    {
        if (MatchManager.Instance != null)
        {
            ServerRoomManager.Instance.LoginAccount(username, password, OnAccountAuthenticated);
        }
    }

    private void OnAccountAuthenticated()
    {
        Debug.Log("User is now authenticated!");
        loggedName.text = username;
        SwapLayers(activeLayer, layerFive);
        loggedIn = true;
    }

    public void EnterIP()
    {
        if (string.IsNullOrEmpty(gameIP))
        {
            Debug.LogError("No room code entered!");
            return;
        }

        if (ServerRoomManager.Instance != null)
        {
            int code;
            if (!int.TryParse(gameIP, out code))
            {
                Debug.LogError("Invalid room code.");
                return;
            }

            ServerRoomManager.Instance.GetServerIpFromRoomCode(code, (serverIp) =>
            {
                if (!string.IsNullOrEmpty(serverIp))
                {
                    Debug.Log("Connecting to IP: " + serverIp);
                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.ConnectionData.Address = serverIp;
                    matchID.text = code.ToString();
                    NetworkManager.Singleton.StartClient();
                    StartCoroutine(HideUIAfterConnection());
                }
                else
                {
                    Debug.LogError("Room not found!");
                }
            });
        }
    }

    private IEnumerator HideUIAfterConnection()
    {
        while (!NetworkManager.Singleton.IsClient)
        {
            yield return null;
        }

        SwapLayers(layerTwo, layerThree);
    }

    public void Ready()
    {
        isReady = !isReady;
        readyButton.GetComponentInChildren<Image>().color = isReady ? Color.green : Color.red;

        if (MatchManager.Instance != null && matchManagerReady && MatchManager.Instance.IsSpawned) // <-- check MatchManager spawned
        {
            Debug.Log("[Ready] Sending ready state to server.");
            MatchManager.Instance.SetReadyStateServerRpc(isReady, NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            Debug.LogWarning("Cannot ready — MatchManager not ready yet!");
        }
    }

    void OnApplicationQuit()
    {
        if (IsServer)
        {
            int roomCode = int.Parse(matchID.text);
            ServerRoomManager.Instance.DeleteRoomFromServer(roomCode);
        }
    }

    public void AccountButton()
    {
        if (!loggedIn)
        {
            if (activeLayer != layerFour)
                SwapLayers(activeLayer, layerFour);
            else
                SwapLayers(activeLayer, layerOne);
        }
        else
        {
            if (activeLayer != layerFive)
                SwapLayers(activeLayer, layerFive);
            else
                SwapLayers(activeLayer, layerOne);
        }
    }

    public void CreateDedicatedServer()
    {
        StartCoroutine(StartDedicatedServer());
    }

    private IEnumerator StartDedicatedServer()
    {
        UnityWebRequest request = new UnityWebRequest("https://zendevfyp.click:3000/start-dedicated-server", "POST");
        request.downloadHandler = new DownloadHandlerBuffer(); 

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<ServerInfo>(request.downloadHandler.text);
            Debug.Log($"Received server: {response.ip}:{response.port}");

            ConnectToServer(response.ip, response.port);
            SwapLayers(activeLayer, layerThree);
        }
        else
        {
            Debug.LogError("Failed to start server: " + request.error);
        }
    }

    private void ConnectToServer(string ip, int port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = (ushort)port;
        NetworkManager.Singleton.StartClient();
    }

    public void ListServers()
    {
        StartCoroutine(GetAvailableServers());
        SwapLayers(activeLayer, layerSix);
    }

    private IEnumerator GetAvailableServers()
    {
        if (serverList.options.Count > 1)
        {
            for (int i = 1; i < serverList.options.Count; i++)
            {
                serverList.options.RemoveAt(i);
            }
        }

        UnityWebRequest request = UnityWebRequest.Get("https://zendevfyp.click:3000/list-dedicated-servers");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Server response: " + request.downloadHandler.text);
            var response = JsonUtility.FromJson<ServerList>(FixJson(request.downloadHandler.text));
            serverInfo = new ServerInfo[response.servers.Length];
            if (response.servers.Length > 0)
            {
                Debug.Log("hahdahdshksa");
                for (int i = 0; i < response.servers.Length; i++)
                {
                    serverList.options[serverList.value].text = "Select Server";
                    TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData();
                    newOption.text = "<size=24>IP: " + response.servers[i].ip + " | PORT: " + response.servers[i].port;
                    serverInfo[i] = response.servers[i];
                    serverList.options.Add(newOption);
                }
            }
        }
        else
        {
            Debug.LogError("Failed to get server list: " + request.error);
        }
    }

    public void JoinDedicatedServer()
    {
        ConnectToServer(serverInfo[serverList.value - 1].ip, serverInfo[serverList.value - 1].port);
        SwapLayers(activeLayer, layerThree);
    }

    private string FixJson(string value)
    {
        value = "{\"servers\":" + value + "}";
        return value;
    }

    private void TrySpawnMatchManager()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (FindObjectOfType<MatchManager>() == null)
            {
                var matchManager = Instantiate(matchManagerPrefab);
                matchManager.GetComponent<NetworkObject>().Spawn(true);
                Debug.Log("[SERVER] Spawned MatchManager manually after server started.");
                firstRun = false;
            }
        }
    }

    private void HandleMatchManagerReady() // <-- Added
    {
        Debug.Log("[NetworkUI] MatchManager networked and ready!");
        matchManagerReady = true;
    }

    public void Return()
    {
        if (activeLayer == layerThree && NetworkManager.IsHost)
        {
            int roomCode = int.Parse(matchID.text);
            ServerRoomManager.Instance.DeleteRoomFromServer(roomCode);
            NetworkManager.Singleton.Shutdown();
        }

        SwapLayers(activeLayer, prevLayer);
    }

    public void QuitMatch()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
