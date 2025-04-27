using UnityEngine;
using System.Diagnostics;
using TMPro.Examples;

public class HostManager : MonoBehaviour
{
    // Start hosting on EC2
    public void StartHostingGame(int roomCode)
    {
        // Path to the bash script on your EC2 server
        string scriptPath = "/home/ubuntu/host_game/start_game.sh";
        string arguments = $"{roomCode}";  // Pass roomCode as argument

        // Create a new process to run the bash script on the EC2 instance via SSH
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = "ssh",  // Use SSH to access the EC2 instance
            Arguments = $"ubuntu@13.51.167.138 'bash {scriptPath} {arguments}'",  // Replace with your EC2 public IP
            RedirectStandardOutput = true,  // Optionally capture output (like the IP)
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Start the process and capture the output (EC2's public IP)
        Process process = new Process { StartInfo = processInfo };
        process.Start();

        // Optionally capture the output (EC2 public IP)
        string output = process.StandardOutput.ReadToEnd();
        UnityEngine.Debug.Log($"Output from EC2: {output}");

        process.WaitForExit();
        if (string.IsNullOrEmpty(output))
        {
            UnityEngine.Debug.LogError("Error: No output from EC2");
        }
        else
        {
            // Log the server IP
            string serverIp = output.Trim();  // Assuming output is just the IP
            UnityEngine.Debug.Log($"Server started on EC2 with IP: {serverIp}");

            // Save the server IP and room code to the database
            UnityEngine.Debug.Log("Room Code: " + roomCode + " | Server IP: " + serverIp);
            CreateRoomOnServer(roomCode, serverIp);
        }
    }

    // Function to save the room code and EC2 server IP to the database
    private void CreateRoomOnServer(int roomCode, string serverIp)
    {
        // Implement the call to your server to save the room code and server IP to the database
        ServerRoomManager.Instance.CreateRoomOnServer(roomCode, serverIp);  // Call the CreateRoomOnServer function in MatchManager
    }
}