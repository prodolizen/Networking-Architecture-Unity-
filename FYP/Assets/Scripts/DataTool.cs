using System;
using System.Collections;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class DataTool : MonoBehaviour
{
    public static DataTool Instance;

    [Tooltip("How often (in seconds) to sample RTT + jitter on the client")]
    public float sampleInterval = 1f;

    private string _csvPath;
    private ulong _localClientId;
    private float _lastReceivedRtt = -1f;
    private float _lastJitter = 0f;
    private bool _isSampling = false;
    private float _lastReconciliationError = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[DataTool] No NetworkManager found!");
            enabled = false;
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[DataTool] Server doesn't sample RTT — disabling.");
            enabled = false;
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (NetworkManager.Singleton.IsConnectedClient)
            OnClientConnected(NetworkManager.Singleton.LocalClientId);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId) //when client connects we record time and create .csv file, then fill .csv
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        _localClientId = clientId;

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fname = $"client_stats_{stamp}.csv";
        _csvPath = Path.Combine(Application.persistentDataPath, fname);
        File.WriteAllText(_csvPath, "Timestamp,ClientId,PingMs,JitterMs,ReconciliationError\n");

        Debug.Log($"[DataTool] Writing to CSV: {_csvPath}");

        _isSampling = true;
        StartCoroutine(SampleLoop());
    }

    private IEnumerator SampleLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);

        while (_isSampling && NetworkManager.Singleton.IsConnectedClient)
        {
            var timestamp = DateTime.UtcNow.ToString("o");
            var line = $"{timestamp},{_localClientId},{_lastReceivedRtt:F1},{_lastJitter:F1},{_lastReconciliationError:F3}\n";
            File.AppendAllText(_csvPath, line);
            yield return wait;
        }
    }

    public void UpdateRttFromServer(float newRtt)
    {
        _lastJitter = _lastReceivedRtt >= 0 ? Mathf.Abs(newRtt - _lastReceivedRtt) : 0f;
        _lastReceivedRtt = newRtt;
    }

    public void ReportReconciliationError(float error)
    {
        _lastReconciliationError = error;
    }
}
