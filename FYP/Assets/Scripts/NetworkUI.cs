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
    public GameObject settings;

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
    private bool settingsMenuUp;

    private void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1) //ensure there is only one instance of network manager
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        MatchManager.OnMatchManagerReady += HandleMatchManagerReady; 
    }

    private void OnDisable()
    {
        MatchManager.OnMatchManagerReady -= HandleMatchManagerReady; 
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
        matchID.text = " ";
    }

    void Update()
    {
        crosshairImage.color = Globals.CrosshairColour; //set crosshair colour

        if (MatchManager.Instance != null && matchManagerReady)
        {
            clientCount.text = "Connected Clients: " + MatchManager.Instance.connectedClients.Value; //display connected clients
            
            if (!settingsMenuUp)
            {
                background.gameObject.SetActive(!MatchManager.Instance.matchActive.Value); //only control background visibility in update if the settings menu is not up
            }

            if (MatchManager.Instance.matchActive.Value) //hide ui on match start
            {
                uiComps.SetActive(false);
                score.SetActive(true); 
            }
            crosshair.SetActive(MatchManager.Instance.matchActive.Value);

            if (self != null && other != null) //set score to correct values
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
                    if (FindObjectOfType<MatchManager>() == null) //ensure there is a match manager, this should be controlled by the server
                    {
                        var matchManager = Instantiate(matchManagerPrefab);
                        matchManager.GetComponent<NetworkObject>().Spawn(true);
                    }
                }
                firstRun = false;
            }
        }

        if (activeLayer == layerThree) //connecting screen
        {
            layerThree.SetActive(MatchManager.Instance != null);
            connecting.gameObject.SetActive(MatchManager.Instance == null);
        }

        if (MatchManager.Instance != null) //find references to both players present
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

            if (MatchManager.Instance.matchEnd.Value) //match end screen
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

            if (MatchManager.Instance.matchActive.Value) //settings menu
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    settingsMenuUp = !settingsMenuUp;
                    settings.SetActive(settingsMenuUp);
                    background.gameObject.SetActive(settingsMenuUp);
                }

                if (settingsMenuUp)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else
                settings.SetActive(false);

        }
    }

    public void StartHost() //start host locally (same machine connectivity) 
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("A host is already running!");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.StartHost();

        if (NetworkManager.Singleton.IsServer) //as we are now the server we can load the scene
        {
            SceneLoader.Instance.LoadScene("Arena", LoadSceneMode.Additive);
        }

        if (MatchManager.Instance != null) //generate random room code that doesnt already exist and bind it to the server adress, then store on SQLite database
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

    private void OnClientConnected(ulong clientId) //development debug
    {
        Debug.Log($"Client {clientId} connected.");
    }

    private void SwapLayers(GameObject layer1, GameObject layer2) //helper function for swapping between UI layers
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

    public void Register() //call server post auth request to register an account
    {
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.RegisterAccount(username, password, OnAccountAuthenticated);
        }
    }

    public void Login() //login to existing
    {
        if (ServerRoomManager.Instance != null)
        {
            ServerRoomManager.Instance.LoginAccount(username, password, OnAccountAuthenticated);
        }
    }

    private void OnAccountAuthenticated() //if we have successfully logged in we set our username in the accounts page
    {
        Debug.Log("User is now authenticated!");
        loggedName.text = username;
        SwapLayers(activeLayer, layerFive);
        loggedIn = true;
    }

    public void EnterIP() //join a local machine hosted game
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

            ServerRoomManager.Instance.GetServerIpFromRoomCode(code, (serverIp) => //look through SQLite database to find the entered roomcode, if it exists and theres an IP linked to it we connect
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

    private IEnumerator HideUIAfterConnection() //hide UI after we are loaded into the game, small delay to ensure everything is finished loading before we do this
    {
        while (!NetworkManager.Singleton.IsClient)
        {
            yield return null;
        }

        SwapLayers(layerTwo, layerThree);
    }

    public void Ready() //ready up button function
    {
        isReady = !isReady;
        readyButton.GetComponentInChildren<Image>().color = isReady ? Color.green : Color.red; //change button colour

        if (MatchManager.Instance != null && matchManagerReady && MatchManager.Instance.IsSpawned) //check MatchManager spawned
        {
            Debug.Log("[Ready] Sending ready state to server.");
            MatchManager.Instance.SetReadyStateServerRpc(isReady, NetworkManager.Singleton.LocalClientId); //send our local ready state to the server to let it handle the logic (maintain server authoritative architecture) 
        }
        else
        {
            Debug.LogWarning("Cannot ready — MatchManager not ready yet!");
        }
    }

    void OnApplicationQuit()
    {
        if (IsServer) //have the server delete the roomcode from the database
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

    private IEnumerator StartDedicatedServer() //manual function to host the dedicated server
    {
        UnityWebRequest request = new UnityWebRequest("https://zendevfyp.click:3000/start-dedicated-server", "POST"); //send web request to correct adress and location
        request.downloadHandler = new DownloadHandlerBuffer(); 

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) //if successful we connect ourselves to this server
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
        if (serverList.options.Count > 1) //clear current dropdown options list
        {
            for (int i = 1; i < serverList.options.Count; i++)
            {
                serverList.options.RemoveAt(i);
            }
        }

        UnityWebRequest request = UnityWebRequest.Get("https://zendevfyp.click:3000/list-dedicated-servers"); //point to correct adress and location
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) //if the request was successful we update the dropdown to display that current server instance
        {
            Debug.Log("Server response: " + request.downloadHandler.text);
            var response = JsonUtility.FromJson<ServerList>(FixJson(request.downloadHandler.text)); //ensure the result is in the required format
            serverInfo = new ServerInfo[response.servers.Length];
            if (response.servers.Length > 0)
            {
                Debug.Log("hahdahdshksa");
                for (int i = 0; i < response.servers.Length; i++)
                {
                    serverList.options[serverList.value].text = "Select Server"; //set the first dropdown option
                    TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData();
                    newOption.text = "<size=24>IP: " + response.servers[i].ip + " | PORT: " + response.servers[i].port; //format and  fill other dropdown options
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

    private string FixJson(string value) //helper function to ensure that the json data we recieve is in the correct format and therefore usable. 
    {
        value = "{\"servers\":" + value + "}";
        return value;
    }

    private void TrySpawnMatchManager() //attempt to spawn a matchmanager object correctly on the network if there isnt one already 
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

    private void HandleMatchManagerReady() //used to alert listeners whether the matchmanager is spawned and connected before doing logic that involves it 
    {
        Debug.Log("[NetworkUI] MatchManager networked and ready!");
        matchManagerReady = true;
    }

    public void Return() //simple return to previous page button 
    {
        if (activeLayer == layerThree && NetworkManager.IsHost)
        {
            int roomCode = int.Parse(matchID.text);
            ServerRoomManager.Instance.DeleteRoomFromServer(roomCode);
            NetworkManager.Singleton.Shutdown();
        }

        SwapLayers(activeLayer, prevLayer);
    }

    public void QuitMatch() //quit button, different logic required whether we are in build or in editor 
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ChangeSensitivity(float f) //function to hook up sensitivity slider to our global sensitivity value. 
    {
        Debug.Log(f);
        Globals.PlayerCamSensX = f;
        Globals.PlayerCamSensY = f;
    }
}
