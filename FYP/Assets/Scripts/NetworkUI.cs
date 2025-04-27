using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Collections;
using System;
using UnityEngine.Networking;

[Serializable]
public class ServerInfo
{
    public string ip;
    public int port;
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
    private string gameIP;
    public GameObject crosshair;
    private Image crosshairImage;
    public TMP_Text clientCount;
    public TMP_Text matchID;
    private bool isReady = false;
    public Button readyButton;
    public Image background;
    private GameObject activeLayer;
    private string username;
    private string password;
    public TMP_Text loggedName;
    private bool loggedIn = false;
    public HostManager hostManager;
    public TMP_Dropdown serverList;
    private ServerInfo[] serverInfo;
    private bool firstRun;
    public GameObject matchManagerPrefab;

    void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        crosshairImage = crosshair.GetComponent<Image>();
        activeLayer = layerOne;

        if (hostManager == null)
        {
            hostManager = FindObjectOfType<HostManager>();
        }
        firstRun = true;
        NetworkManager.Singleton.OnServerStarted += TrySpawnMatchManager;
    }

    void Update()
    {
        crosshairImage.color = Globals.CrosshairColour;

        //crosshair.SetActive(MatchManager.Instance.matchActive.Value);

        //if (Input.GetKey(KeyCode.Alpha7))
        //{
        //    StartHost();
        //}

        if (MatchManager.Instance != null)
        {
            clientCount.text = "Connected Clients: " + MatchManager.Instance.connectedClients.Value;
            background.gameObject.SetActive(!MatchManager.Instance.matchActive.Value);
            Debug.Log("glelo");
            //hide ui on match start
            if (MatchManager.Instance.matchActive.Value)
            {
                uiComps.SetActive(false);
            }
        }

        else
        {
            if (firstRun)
            {
                Debug.Log("hell");

                // Instead of listening for OnServerStarted every frame, check directly:
                if (NetworkManager.Singleton.IsServer)
                {
                    if (FindObjectOfType<MatchManager>() == null)
                    {
                        Debug.Log("[SERVER] Spawning MatchManager manually because it's missing...");
                        var matchManager = Instantiate(matchManagerPrefab);
                        matchManager.GetComponent<NetworkObject>().Spawn(true);
                    }
                }
                firstRun = false;
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
            // string ip = MatchManager.Instance.serverAdress.Value.ToString();

            string ip = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;
            MatchManager.Instance.serverAdress.Value = ip;

            Debug.Log("Setting server IP: " + ip);

            matchID.text = roomCode.ToString();
            ServerRoomManager.Instance.CreateRoomOnServer(roomCode, ip);
        }

        SwapLayers(layerOne, layerThree);
    }

    // Button click for hosting gam

    //public void StartHost()
    //{
    //     Disable host creation on local machine, instead start EC2 instance
    //    if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
    //    {
    //        Debug.LogError("A host is already running!");
    //        return;
    //    }

    //     First, we trigger the EC2 server startup
    //    if (hostManager != null)
    //    {
    //        int roomCode = UnityEngine.Random.Range(1000, 9999);

    //         Call HostManager to start the EC2 server and pass the room code
    //        hostManager.StartHostingGame(roomCode);

    //         Save the room code locally to be shared with others
    //        matchID.text = roomCode.ToString();
    //    }

    //     Once EC2 instance is running, allow the user to join the server
    //    SwapLayers(layerOne, layerThree);
    //}


    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected.");
    }

    private void SwapLayers(GameObject layer1, GameObject layer2)
    {
        layer1.SetActive(false);
        layer2.SetActive(true);
        activeLayer = layer2;
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

    //check for successful login
    private void OnAccountAuthenticated()
    {
        Debug.Log("User is now authenticated!");
        // Swap to game layer, enable features, etc.
        loggedName.text = username;
        SwapLayers(activeLayer, layerFive); // Or wherever you want to go after auth
        loggedIn = true;
    }

    //public void EnterIP()
    //{
    //    if (string.IsNullOrEmpty(gameIP))
    //    {
    //        Debug.LogError("No room code entered!");
    //        return;
    //    }

    //    if (MatchManager.Instance != null)
    //    {
    //        int code;
    //        if (!int.TryParse(gameIP, out code))
    //        {
    //            Debug.LogError("Invalid room code.");
    //            return;
    //        }

    //        MatchManager.Instance.GetServerIpFromRoomCode(code, (serverIp) =>
    //        {
    //            if (!string.IsNullOrEmpty(serverIp))
    //            {
    //                Debug.Log("Connecting to IP: " + serverIp);
    //                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    //                transport.ConnectionData.Address = serverIp;
    //                matchID.text = code.ToString();
    //                NetworkManager.Singleton.StartClient();
    //                StartCoroutine(HideUIAfterConnection());
    //            }
    //            else
    //            {
    //                Debug.LogError("Room not found!");
    //            }
    //        });
    //    }
    //}

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

            // Get the server IP using the room code
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

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.SetReadyStateServerRpc(isReady, NetworkManager.Singleton.LocalClientId);
        }
    }
    void OnApplicationQuit() //delete current room code and ip from SQL DB
    {
        if (IsServer) // only host should delete
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
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<ServerInfo>(request.downloadHandler.text);
            ConnectToServer(response.ip, response.port);
        }
        else
        {
            Debug.LogError("Failed to start server: " + request.error);
        }
    }

    private void ConnectToServer(string ip, int port)
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = (ushort)port;
        NetworkManager.Singleton.StartClient();
    }

    public void ListServers()
    {
        Debug.Log("hello");
        StartCoroutine(GetAvailableServers());
        SwapLayers(activeLayer, layerSix);
    }

    private IEnumerator GetAvailableServers()
    {
        UnityWebRequest request = UnityWebRequest.Get("https://zendevfyp.click:3000/list-dedicated-servers");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("haha");
            Debug.Log("Server response: " + request.downloadHandler.text); // ADD THIS

            var response = JsonUtility.FromJson<ServerList>(FixJson(request.downloadHandler.text));
            serverInfo = new ServerInfo[response.servers.Length];
            if (response.servers.Length > 0)
            {
                // ConnectToServer(response.servers[0].ip, response.servers[0].port);
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
}