using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Komponen Player")]
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private ShapePlacementController shapePlacementController;

    [Header("Titik Pegangan Item")]
    [SerializeField] private Transform handPoint;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public override void OnNetworkSpawn()
    {
        bool isLocalPlayer = IsOwner;

        // Hanya Player milik client ini yang menerima input.
        if (playerMovement != null)
            playerMovement.enabled = isLocalPlayer;

        if (characterController != null)
            characterController.enabled = isLocalPlayer;

        if (playerCamera != null)
            playerCamera.enabled = isLocalPlayer;

        if (audioListener != null)
            audioListener.enabled = isLocalPlayer;

        if (shapePlacementController != null)
            shapePlacementController.enabled = isLocalPlayer;

        // Inventory UI hanya dihubungkan ke Player lokal.
        if (isLocalPlayer)
        {
            StartCoroutine(ConnectLocalInventory());
        }
    }

    private IEnumerator ConnectLocalInventory()
    {
        Inventory localInventory = null;

        // Player dapat terbentuk sebelum scene Gameplay selesai dimuat.
        // Karena itu, tunggu sampai Inventory tersedia.
        while (localInventory == null)
        {
            if (SceneManager.GetActiveScene().name == gameplaySceneName)
            {
                localInventory = FindFirstObjectByType<Inventory>();
            }

            if (localInventory == null)
                yield return null;
        }

        if (handPoint == null)
        {
            Debug.LogError(
                "Hand Point belum dihubungkan pada NetworkPlayerSetup."
            );

            yield break;
        }

        // Hubungkan Inventory UI ke tangan Player lokal.
        localInventory.BindToPlayer(handPoint);

        // Hubungkan sistem placement dengan Inventory lokal.
        if (shapePlacementController != null)
        {
            shapePlacementController.SetInventory(localInventory);
        }

        Debug.Log("Player lokal siap menggunakan Inventory.");
    }
}