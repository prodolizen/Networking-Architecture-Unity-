using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Net;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;

public class NetworkUI : MonoBehaviour
{
    public GameObject uiComps;
    public GameObject layerOne;
    public GameObject layerTwo;
    private string gameIP;
    public GameObject crosshair;
    private Image crosshairImage;
    // Start is called before the first frame update
    void Start()
    {
        crosshairImage = crosshair.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        crosshairImage.color = Globals.CrosshairColour;
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            Debug.LogError("CANT HAVE 2 HOSTS CUH");
        }

        uiComps.SetActive(false);
        crosshair.SetActive(true);
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
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = gameIP;
        NetworkManager.Singleton.StartClient();
        uiComps.SetActive(false);
        crosshair.SetActive(true);
    }
}
