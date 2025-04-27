using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro.Examples;

public class TestServer : MonoBehaviour
{
    void Start()
    {
        Debug.Log("heslrfesjl");
        StartCoroutine(PostRoom());
    }

    IEnumerator PostRoom()
    {
        string url = "http://zendevfyp.click:3000/create-room"; 
        string json = "{\"roomCode\":4321, \"serverIp\":\"123.456.789.000\"}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
    }
}
