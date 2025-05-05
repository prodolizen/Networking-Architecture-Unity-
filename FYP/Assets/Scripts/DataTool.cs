using System;
using System.Collections;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class DataTool : MonoBehaviour
{
    [Tooltip("How often (in seconds) to sample RTT + jitter on the client")]
    public float sampleInterval = 1f;

    private UnityTransport _transport;
    private string _csvPath;
    private ulong _localClientId;
    private float _lastRtt = -1f;
    private bool _isSampling = false;

    private void Start()
    {
        // Make sure there's a Netcode manager in the scene
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[DataTool] No NetworkManager found in scene!");
            enabled = false;
            return;
        }

        // Only run on clients (including host)
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[DataTool] Not a client → disabling.");
            enabled = false;
            return;
        }

        // Defer initialization until we actually connect
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // In host mode, we may already be “connected”
        if (NetworkManager.Singleton.IsConnectedClient)
            OnClientConnected(NetworkManager.Singleton.LocalClientId);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only initialize for our own client
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        // Grab the transport & our client ID
        _transport = NetworkManager.Singleton
            .NetworkConfig
            .NetworkTransport as UnityTransport;
        _localClientId = clientId;

        // Prepare CSV file
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fname = $"client_stats_{stamp}.csv";
        _csvPath = Path.Combine(Application.persistentDataPath, fname);

        // Write header row
        File.WriteAllText(_csvPath, "Timestamp,ClientId,PingMs,JitterMs\n");
        Debug.Log($"[DataTool] Writing CSV to: {_csvPath}");

        // Start sampling loop
        _isSampling = true;
        StartCoroutine(SampleLoop());
    }

    private IEnumerator SampleLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);

        while (_isSampling && NetworkManager.Singleton.IsConnectedClient)
        {
            var timestamp = DateTime.UtcNow.ToString("o");

            // 1) get current RTT
            float rtt = 0f;
            if (_transport != null)
                rtt = (float)_transport.GetCurrentRtt(_localClientId);

            // 2) compute jitter = abs(diff from last sample)
            float jitter = _lastRtt >= 0f
                ? Mathf.Abs(rtt - _lastRtt)
                : 0f;
            _lastRtt = rtt;

            // 3) append line
            var line = $"{timestamp},{_localClientId},{rtt:F1},{jitter:F1}\n";
            File.AppendAllText(_csvPath, line);

            yield return wait;
        }
    }
}
