using UnityEngine;

public class ShapePlacementController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private Camera playerCamera;

    [Header("Raycast Placement")]
    [SerializeField] private float interactionDistance = 4f;
    [SerializeField] private LayerMask placementSlotLayer;

    [Header("Throw")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 8f;

    [Header("Game Manager")]
    [SerializeField] private PuzzleGameManager gameManager;

    private ShapeSlot currentSlot;

    

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // Ambil Inventory pada object yang sama kalau belum diisi
        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
        }

        // Ambil Camera Main kalau belum diisi
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Pengaman
        if (inventory == null)
            return;

        if (playerCamera == null)
            return;
            
        if (gameManager != null && gameManager.IsGameEnded)
        {
           ClearCurrentSlot();
           return;
        }
        // Kalau inventory sedang terbuka,
        // placement tidak boleh berjalan
        if (inventory.IsInventoryOpen)
        {
            ClearCurrentSlot();
            return;
        }

        // Update slot yang sedang dilihat
        UpdateSlotHighlight();

        // =====================================================
        // LEFT CLICK = PLACE
        // =====================================================

        if (Input.GetMouseButtonDown(0))
        {
            TryPlace();
        }

        // =====================================================
        // RIGHT CLICK = THROW
        // =====================================================

        if (Input.GetMouseButtonDown(1))
        {
            TryThrow();
        }


    }

    // =========================================================
    // DETECT SLOT
    // =========================================================

    private void UpdateSlotHighlight()
    {
        // Ambil item yang sedang dipilih di hotbar
        itemSo equippedItem = inventory.GetEquippedItem();

        // Tidak ada item yang dipilih
        if (equippedItem == null)
        {
            ClearCurrentSlot();
            return;
        }

        // Item tidak punya prefab fisik
        if (equippedItem.itemPrefab == null)
        {
            ClearCurrentSlot();
            return;
        }

        // Cari ShapeObject dari prefab fisik
        ShapeObject shapePrefab =
            equippedItem.itemPrefab.GetComponent<ShapeObject>();

        if (shapePrefab == null)
        {
            ClearCurrentSlot();
            return;
        }

        // Ray dari tengah kamera
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        ShapeSlot detectedSlot = null;

        // Cari slot yang sedang dilihat
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            placementSlotLayer,
            QueryTriggerInteraction.Collide))
        {
            detectedSlot =
                hit.collider.GetComponentInParent<ShapeSlot>();
        }

        // Kalau slot berubah
        if (detectedSlot != currentSlot)
        {
            ClearCurrentSlot();

            currentSlot = detectedSlot;
        }

        // Tidak ada slot
        if (currentSlot == null)
            return;

        // =====================================================
        // CEK APAKAH BENTUK COCOK
        // =====================================================

        bool canPlace =
            currentSlot.CanPlace(shapePrefab);

        if (canPlace)
        {
            // 1 = valid = hijau
            currentSlot.SetHighlight(1);
        }
        else
        {
            // 2 = invalid = merah
            currentSlot.SetHighlight(2);
        }
    }

    // =========================================================
    // PLACE OBJECT
    // =========================================================

    private void TryPlace()
    {
        // Tidak ada slot
        if (currentSlot == null)
            return;

        // Ambil item yang sedang dipilih
        itemSo equippedItem =
            inventory.GetEquippedItem();

        if (equippedItem == null)
            return;

        // Tidak ada prefab fisik
        if (equippedItem.itemPrefab == null)
            return;

        // Ambil ShapeObject dari prefab
        ShapeObject shapePrefab =
            equippedItem.itemPrefab.GetComponent<ShapeObject>();

        if (shapePrefab == null)
        {
            Debug.LogWarning(
                "Item " + equippedItem.itemName +
                " tidak memiliki ShapeObject."
            );

            return;
        }

        // =====================================================
        // CEK APAKAH SLOT BENAR
        // =====================================================

        if (!currentSlot.CanPlace(shapePrefab))
        {
            // Salah bentuk atau slot sudah terisi
            return;
        }

        // =====================================================
        // BUAT OBJECT FISIK
        // =====================================================

        GameObject spawnedObject =
            Instantiate(equippedItem.itemPrefab);

        ShapeObject spawnedShape =
            spawnedObject.GetComponent<ShapeObject>();

        if (spawnedShape == null)
        {
            Debug.LogWarning(
                "Prefab " +
                equippedItem.itemName +
                " tidak memiliki ShapeObject."
            );

            Destroy(spawnedObject);
            return;
        }

        // =====================================================
        // MASUKKAN KE SLOT
        // =====================================================

        currentSlot.PlaceObject(spawnedShape);

        // =====================================================
        // KURANGI INVENTORY 1 ITEM
        // =====================================================

        bool removed =
            inventory.RemoveEquippedItem(1);

        // Kalau pengurangan inventory gagal,
        // object yang sudah dibuat kita hapus lagi
        if (!removed)
        {
            Destroy(spawnedObject);

            Debug.LogWarning(
                "Gagal mengurangi item dari inventory."
            );

            return;
        }

        // Hilangkan highlight
        ClearCurrentSlot();
    }

    // =========================================================
    // THROW OBJECT
    // =========================================================

    private void TryThrow()
    {
        // Ambil item yang sedang dipilih
        itemSo equippedItem =
            inventory.GetEquippedItem();

        if (equippedItem == null)
            return;

        // Tidak ada prefab fisik
        if (equippedItem.itemPrefab == null)
            return;

        // Pastikan prefab mempunyai ShapeObject
        ShapeObject shapePrefab =
            equippedItem.itemPrefab.GetComponent<ShapeObject>();

        if (shapePrefab == null)
        {
            Debug.LogWarning(
                "Item " +
                equippedItem.itemName +
                " tidak memiliki ShapeObject."
            );

            return;
        }

        // =====================================================
        // POSISI THROW
        // =====================================================

        Vector3 spawnPosition;

        if (throwPoint != null)
        {
            spawnPosition = throwPoint.position;
        }
        else
        {
            spawnPosition =
                playerCamera.transform.position +
                playerCamera.transform.forward * 1.2f;
        }

        // =====================================================
        // BUAT OBJECT
        // =====================================================

        GameObject spawnedObject =
            Instantiate(
                equippedItem.itemPrefab,
                spawnPosition,
                Quaternion.identity
            );

        ShapeObject spawnedShape =
            spawnedObject.GetComponent<ShapeObject>();

        if (spawnedShape == null)
        {
            Destroy(spawnedObject);
            return;
        }

        // =====================================================
        // LEMPAR
        // =====================================================

        spawnedShape.Throw(
            playerCamera.transform.forward,
            throwForce
        );

        // =====================================================
        // KURANGI INVENTORY 1 ITEM
        // =====================================================

        bool removed =
            inventory.RemoveEquippedItem(1);

        if (!removed)
        {
            Destroy(spawnedObject);

            Debug.LogWarning(
                "Gagal mengurangi item dari inventory."
            );

            return;
        }

        // Hilangkan highlight
        ClearCurrentSlot();
    }

    // =========================================================
    // CLEAR HIGHLIGHT
    // =========================================================

    private void ClearCurrentSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.SetHighlight(0);
            currentSlot = null;
        }
    }

    public void SetInventory(Inventory newInventory)
{
    inventory = newInventory;
}
}