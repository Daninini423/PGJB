
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerSpawnManager : MonoBehaviour
{
    [Header("Player Spawn Points")]
    [SerializeField] private Transform p1;
    [SerializeField] private Transform p2;

    private IEnumerator Start()
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("NetworkManager tidak ditemukan!");
            yield break;
        }

        // Hanya Host/Server yang menentukan posisi spawn.
        if (!networkManager.IsServer)
            yield break;

        if (p1 == null || p2 == null)
        {
            Debug.LogError(
                "P1 atau P2 belum dihubungkan di Inspector."
            );
            yield break;
        }

        // Tunggu sampai PlayerObject setiap client tersedia.
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool allPlayersReady = true;

            foreach (ulong clientId in
                     networkManager.ConnectedClientsIds)
            {
                if (!networkManager.ConnectedClients
                        .TryGetValue(clientId, out var client) ||
                    client.PlayerObject == null)
                {
                    allPlayersReady = false;
                    break;
                }
            }

            if (allPlayersReady)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Urutkan ID agar Host (biasanya ID 0) mendapat P1.
        List<ulong> clientIds = new List<ulong>(
            networkManager.ConnectedClientsIds
        );

        clientIds.Sort();

        if (clientIds.Count < 2)
        {
            Debug.LogWarning(
                "Belum ada dua pemain yang terhubung."
            );
            yield break;
        }

        Transform[] spawnPoints = { p1, p2 };

        for (int i = 0; i < 2 && i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];

            if (!networkManager.ConnectedClients.TryGetValue(
                    clientId, out var client))
            {
                continue;
            }

            NetworkObject player = client.PlayerObject;

            if (player == null)
            {
                Debug.LogError(
                    "PlayerObject tidak ditemukan untuk Client ID "
                    + clientId
                );
                continue;
            }

            player.transform.SetPositionAndRotation(
                spawnPoints[i].position,
                spawnPoints[i].rotation
            );

            Debug.Log(
                "Client " + clientId +
                " spawn di " + spawnPoints[i].name
            );
        }
    }
}