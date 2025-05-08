using UnityEngine;
using System.Diagnostics;
using TMPro.Examples;

public class HostManager : MonoBehaviour
{
    // Start hosting on EC2
    public void StartHostingGame(int roomCode)
    {
        string scriptPath = "/home/ubuntu/host_game/start_game.sh";
        string arguments = $"{roomCode}";  // Pass roomCode as argument

        // Create a new process to run the bash script on the EC2 instance via SSH
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = "ssh",  // Use SSH to access the EC2 instance
            Arguments = $"ubuntu@13.51.167.138 'bash {scriptPath} {arguments}'",  
            RedirectStandardOutput = true, 
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Start the process and capture the output 
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
            string serverIp = output.Trim();  //
            UnityEngine.Debug.Log($"Server started on EC2 with IP: {serverIp}");

            // Save the server IP and room code to the database
            UnityEngine.Debug.Log("Room Code: " + roomCode + " | Server IP: " + serverIp);
            CreateRoomOnServer(roomCode, serverIp);
        }
    }

    // Function to save the room code and EC2 server IP to the database
    private void CreateRoomOnServer(int roomCode, string serverIp)
    {
        ServerRoomManager.Instance.CreateRoomOnServer(roomCode, serverIp); 
    }
}