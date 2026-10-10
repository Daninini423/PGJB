using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkConnectionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_Text statusText;

    [Header("LAN Settings")]
    [SerializeField] private ushort port = 7777;

    [Header("Game")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private int playersToStart = 2;

    private UnityTransport transport;

    private bool gameplayStarting = false;

    private void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "NetworkManager tidak ditemukan!"
            );

            return;
        }

        transport =
            NetworkManager.Singleton
                .GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError(
                "Unity Transport tidak ditemukan!"
            );
        }

        // Dengarkan player yang masuk
        NetworkManager.Singleton.OnClientConnectedCallback
            += OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback
            += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback
                -= OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback
                -= OnClientDisconnected;
        }
    }

    // =========================================================
    // HOST
    // =========================================================

    public void StartHost()
    {
        Debug.Log("TOMBOL HOST DITEKAN");

        if (NetworkManager.Singleton == null)
            return;

        if (transport == null)
            return;

        if (NetworkManager.Singleton.IsListening)
            return;

        // Host menerima koneksi dari LAN
        transport.SetConnectionData(
            "0.0.0.0",
            port,
            "0.0.0.0"
        );

        bool success =
            NetworkManager.Singleton.StartHost();

        if (success)
        {
            Debug.Log("Host berhasil dimulai.");

            SetStatus(
                "Menunggu Player 2..."
            );
        }
        else
        {
            Debug.LogError(
                "Gagal memulai Host."
            );

            SetStatus(
                "Host gagal dimulai."
            );
        }
    }

    // =========================================================
    // CLIENT
    // =========================================================

    public void StartClient()
    {
        Debug.Log("TOMBOL JOIN DITEKAN");
        
        if (NetworkManager.Singleton == null)
            return;

        if (transport == null)
            return;

        if (NetworkManager.Singleton.IsListening)
            return;

        if (ipInput == null)
        {
            Debug.LogError(
                "IpInput belum dihubungkan."
            );

            return;
        }

        string hostIP =
            ipInput.text.Trim();

        if (string.IsNullOrEmpty(hostIP))
        {
            SetStatus(
                "Masukkan IP Host!"
            );

            return;
        }

        // Hubungkan ke IP Host
        transport.SetConnectionData(
            hostIP,
            port
        );

        SetStatus(
            "Menghubungkan ke Host..."
        );

        bool success =
            NetworkManager.Singleton.StartClient();

        if (!success)
        {
            Debug.LogError(
                "Gagal memulai Client."
            );

            SetStatus(
                "Client gagal dimulai."
            );
        }
    }

    // =========================================================
    // CLIENT CONNECTED
    // =========================================================

    private void OnClientConnected(ulong clientId)
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
            return;

        Debug.Log(
            "Client connected: " + clientId
        );

        // -----------------------------------------------------
        // Kalau ini adalah CLIENT biasa
        // -----------------------------------------------------

        if (!networkManager.IsServer)
        {
            if (clientId == networkManager.LocalClientId)
            {
                SetStatus(
                    "Berhasil terhubung ke Host!"
                );

                // Sembunyikan panel.
                // Client akan menunggu server
                // mengganti scene.
                gameObject.SetActive(false);
            }

            return;
        }

        // -----------------------------------------------------
        // Kalau ini HOST / SERVER
        // -----------------------------------------------------

        int connectedPlayers =
            networkManager.ConnectedClientsIds.Count;

        Debug.Log(
            "Jumlah player: " +
            connectedPlayers
        );

        SetStatus(
            "Player terhubung: " +
            connectedPlayers +
            "/" +
            playersToStart
        );

        // Cek apakah jumlah player sudah cukup
        if (connectedPlayers >= playersToStart)
        {
            StartGameplay();
        }
    }

    // =========================================================
    // START GAMEPLAY
    // =========================================================

    private void StartGameplay()
    {
        if (gameplayStarting)
            return;

        gameplayStarting = true;

        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
            return;

        if (!networkManager.IsServer)
            return;

        Debug.Log(
            "Semua player sudah masuk. " +
            "Memulai Gameplay..."
        );

        // Network Scene Management
        networkManager.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );
    }

    // =========================================================
    // CLIENT DISCONNECTED
    // =========================================================

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
            return;

        // Hanya tampilkan pesan jika masih di menu
        if (!networkManager.IsServer)
        {
            SetStatus(
                "Koneksi terputus."
            );
        }

        Debug.Log(
            "Client disconnected: " +
            clientId
        );
    }

    // =========================================================
    // STATUS UI
    // =========================================================

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(
            "[Network] " + message
        );
    }
}