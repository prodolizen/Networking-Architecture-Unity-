using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Net;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkUI : NetworkBehaviour
{
    public GameObject uiComps;
    public GameObject layerOne;
    public GameObject layerTwo;
    public GameObject layerThree;
    private string gameIP;
    public GameObject crosshair;
    private Image crosshairImage;
    public TMP_Text clientCount;
    public TMP_Text matchID;
    private bool isReady = false;
    public Button readyButton;

    void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1)
        {
            Debug.LogError("Duplicate NetworkManager detected! Destroying extra instance.");
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        crosshairImage = crosshair.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        crosshairImage.color = Globals.CrosshairColour;

        if (Input.GetKey(KeyCode.Alpha7))
        {
            StartHost();
        }

        if (MatchManager.Instance != null)
        {
            clientCount.text = "Connected Clients: " + MatchManager.Instance.connectedClients;
        }
    }

    public void StartHost()
    {
        Debug.Log("START HOST");

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

        SwapLayers(layerOne, layerThree);
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected but not spawning player yet.");
    }



    private void SwapLayers(GameObject layer1, GameObject layer2)
    {
        layer1.SetActive(false);
        layer2.SetActive(true);
    }

    public void JoinGame()
    {
        SwapLayers(layerOne, layerTwo);
       
    }

    public void TakeIP(string s)
    {
        gameIP = s;
    }

    public void EnterIP()
    {
        if (string.IsNullOrEmpty(gameIP))
        {
            Debug.LogError("No IP address entered!");
            return;
        }

        Debug.Log("Attempting to connect to " + gameIP);

        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = gameIP;
        NetworkManager.Singleton.StartClient();

        // Wait for client to connect before hiding UI
        StartCoroutine(HideUIAfterConnection());
    }

    private IEnumerator HideUIAfterConnection()
    {
        while (!NetworkManager.Singleton.IsClient)
        {
            yield return null; // Wait until client starts
        }

        Debug.Log("Client successfully started, hiding UI.");
        //uiComps.SetActive(false);
        //crosshair.SetActive(true);
        SwapLayers(layerTwo, layerThree);
    }

    public void Ready()
    {
        isReady = !isReady;

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.SetReadyStateServerRpc(isReady, NetworkManager.Singleton.LocalClientId);
        }

        readyButton.GetComponentInChildren<Text>().text = isReady ? "Unready" : "Ready";
    }
}
