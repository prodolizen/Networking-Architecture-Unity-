using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class ServerRoomManager : MonoBehaviour
{
    private const string baseUrl = "http://13.51.167.138:3000";

    // This will hold the room code and server IP
    private int roomCode;
    private string serverIp;

    void Start()
    {
        // Generate a random room code
        roomCode = UnityEngine.Random.Range(1000, 9999);

        // Get the server's public IP address (assuming it's hosted on EC2 with a static IP)
        serverIp = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;

        // Send the room code and IP to the database
        CreateRoomInDatabase(roomCode, serverIp);
    }

    // This method sends the room code and server IP to the database
    private void CreateRoomInDatabase(int roomCode, string serverIp)
    {
        Debug.Log($"Creating room with Room Code: {roomCode} and Server IP: {serverIp}");

        // Create a room data object to send
        RoomData data = new RoomData { roomCode = roomCode, serverIp = serverIp };
        string json = JsonUtility.ToJson(data); // Use JsonUtility or another serializer

        // Start the POST request to add room to database
        StartCoroutine(PostRequest($"{baseUrl}/create-room", json));
    }

    // Coroutine to send a POST request
    private IEnumerator PostRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Post Error: {request.error}");
        }
        else
        {
            Debug.Log($"Room created: {request.downloadHandler.text}");
        }
    }

    // This class will hold the data to send to the API
    [Serializable]
    public class RoomData
    {
        public int roomCode;
        public string serverIp;
    }
}
