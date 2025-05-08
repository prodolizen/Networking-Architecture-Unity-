using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

//script to grab rtt using unitys methods
public class RTTReporter : NetworkBehaviour
{
    private UnityTransport _transport;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            InvokeRepeating(nameof(SendRTTToClients), 1f, 1f);
        }
    }

    private void SendRTTToClients()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            float rtt = (float)_transport.GetCurrentRtt(clientId);
            SendRTTClientRpc(rtt, clientId);
        }
    }

    [ClientRpc]
    private void SendRTTClientRpc(float rtt, ulong targetClientId = 0, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        DataTool.Instance?.UpdateRttFromServer(rtt);
    }
}
