using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Collections;
using System;

public class NetworkUI : NetworkBehaviour
{
    public GameObject uiComps;
    public GameObject layerOne;
    public GameObject layerTwo;
    public GameObject layerThree;
    public GameObject layerFour;    
    public GameObject layerFive;
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

            //hide ui on match start
            if (MatchManager.Instance.matchActive.Value)
            {
                uiComps.SetActive(false);
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
            MatchManager.Instance.CreateRoomOnServer(roomCode, ip);
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
            MatchManager.Instance.RegisterAccount(username, password, OnAccountAuthenticated);
        }
    }

    public void Login()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.LoginAccount(username, password, OnAccountAuthenticated);
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

        if (MatchManager.Instance != null)
        {
            int code;
            if (!int.TryParse(gameIP, out code))
            {
                Debug.LogError("Invalid room code.");
                return;
            }

            // Get the server IP using the room code
            MatchManager.Instance.GetServerIpFromRoomCode(code, (serverIp) =>
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
            MatchManager.Instance.DeleteRoomFromServer(roomCode);
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
}
