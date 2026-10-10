using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Item Reference")]
    public itemSo woodItem;
    public itemSo AxeItem;

    [Header("Inventory UI")]
    public GameObject hotbarObj;
    public GameObject inventorySlotParent;
    public GameObject container;
    public Image dragIcon;

    [Header("Pickup")]
    public float pickupRange = 3f;
    public Material highlightMaterial;

    [Header("Hand Item")]
    public float equippedOpacity = 0.9f;
    public float normalOpacity = 0.58f;
    public Transform hand;

    // Item yang sedang dilihat
    private Item lookedAtItem = null;
    private Material originalMaterial;
    private Renderer lookedAtRenderer = null;

    // Item yang sedang berada di tangan
    private GameObject currentHandItem;

    // Hotbar
    private int equippedHotBarIndex = 0;

    // Inventory slots
    private List<Slot> inventorySlots = new List<Slot>();
    private List<Slot> hotbarSlots = new List<Slot>();
    private List<Slot> allSlots = new List<Slot>();

    // Drag inventory
    private Slot draggedSlot = null;
    private bool isDraggin = false;

    // =========================================================
    // PROPERTY
    // =========================================================

    public bool IsInventoryOpen
    {
        get
        {
            return container != null &&
                   container.activeInHierarchy;
        }
    }

    // =========================================================
    // UNITY METHODS
    // =========================================================

    private void Awake()
    {
        if (inventorySlotParent != null)
        {
            inventorySlots.AddRange(
                inventorySlotParent.GetComponentsInChildren<Slot>()
            );
        }

        if (hotbarObj != null)
        {
            hotbarSlots.AddRange(
                hotbarObj.GetComponentsInChildren<Slot>()
            );
        }

        allSlots.AddRange(inventorySlots);
        allSlots.AddRange(hotbarSlots);
    }

    private void Update()
    {
        // =====================================================
        // BUKA / TUTUP INVENTORY
        // =====================================================

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (container != null)
            {
                container.SetActive(!container.activeInHierarchy);
            }

            Cursor.lockState =
                Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None
                : CursorLockMode.Locked;

            Cursor.visible = !Cursor.visible;

            if (PlayerCam.Instance != null)
            {
                PlayerCam.Instance.updatingRotation =
                    !PlayerCam.Instance.updatingRotation;
            }
        }

        // =====================================================
        // PICKUP
        // =====================================================

        DetectLookedAtItem();
        Pickup();

        // =====================================================
        // DRAG INVENTORY
        // =====================================================

        StartDrag();
        UpdateDragItemPosition();
        EndDrag();

        // =====================================================
        // HOTBAR
        // =====================================================

        HandleHotBarSelection();
        HandleDropEquippedItem();
        UpdateHotbarOpacity();
    }

    // =========================================================
    // ADD ITEM
    // =========================================================

    public void AddItem(itemSo itemToAdd, int amount)
    {
        if (itemToAdd == null)
        {
            Debug.LogWarning("AddItem gagal: itemToAdd null.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning("AddItem gagal: amount harus lebih dari 0.");
            return;
        }

        int remaining = amount;

        // -----------------------------------------------------
        // Coba masukkan ke stack yang sudah ada
        // -----------------------------------------------------

        foreach (Slot slot in allSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;

                if (currentAmount < maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int amountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(
                        itemToAdd,
                        currentAmount + amountToAdd
                    );

                    // PENTING:
                    // Kurangi remaining setelah item masuk
                    remaining -= amountToAdd;

                    if (remaining <= 0)
                    {
                        EquipHandItem();
                        return;
                    }
                }
            }
        }

        // -----------------------------------------------------
        // Masukkan ke slot kosong
        // -----------------------------------------------------

        foreach (Slot slot in allSlots)
        {
            if (!slot.HasItem())
            {
                int amountToPlace =
                    Mathf.Min(itemToAdd.maxStackSize, remaining);

                slot.SetItem(
                    itemToAdd,
                    amountToPlace
                );

                remaining -= amountToPlace;

                if (remaining <= 0)
                {
                    EquipHandItem();
                    return;
                }
            }
        }

        // -----------------------------------------------------
        // Inventory penuh
        // -----------------------------------------------------

        if (remaining > 0)
        {
            Debug.Log(
                "Inventory is full, could not add "
                + remaining
                + " of "
                + itemToAdd.itemName
            );
        }

        EquipHandItem();
    }

    // =========================================================
    // DRAG SYSTEM
    // =========================================================

    private void StartDrag()
    {
        // Kalau inventory tertutup, jangan drag
        if (!IsInventoryOpen)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Slot hovered = GetHoveredSlot();

            if (hovered != null && hovered.HasItem())
            {
                draggedSlot = hovered;
                isDraggin = true;

                // Tampilkan icon drag
                if (dragIcon != null)
                {
                    dragIcon.sprite = hovered.GetItem().icon;
                    dragIcon.color =
                        new Color(1f, 1f, 1f, 0.5f);

                    dragIcon.enabled = true;
                }
            }
        }
    }

    private void EndDrag()
    {
        // Tidak sedang drag
        if (!isDraggin)
            return;

        // Hanya selesai ketika mouse kiri dilepas
        if (Input.GetMouseButtonUp(0))
        {
            Slot hovered = GetHoveredSlot();

            if (hovered != null && draggedSlot != null)
            {
                HandleDrop(
                    draggedSlot,
                    hovered
                );
            }

            if (dragIcon != null)
            {
                dragIcon.enabled = false;
            }

            draggedSlot = null;
            isDraggin = false;
        }
    }

    private Slot GetHoveredSlot()
    {
        foreach (Slot s in allSlots)
        {
            if (s.hovering)
            {
                return s;
            }
        }

        return null;
    }

    private void HandleDrop(Slot from, Slot to)
    {
        // Pengaman
        if (from == null ||
            to == null ||
            from == to)
        {
            return;
        }

        // =====================================================
        // ITEM SAMA → STACK
        // =====================================================

        if (to.HasItem() &&
            to.GetItem() == from.GetItem())
        {
            int max = to.GetItem().maxStackSize;
            int space = max - to.GetAmount();

            if (space > 0)
            {
                int move =
                    Mathf.Min(
                        space,
                        from.GetAmount()
                    );

                to.SetItem(
                    to.GetItem(),
                    to.GetAmount() + move
                );

                from.SetItem(
                    from.GetItem(),
                    from.GetAmount() - move
                );

                if (from.GetAmount() <= 0)
                {
                    from.ClearSlot();
                }

                EquipHandItem();
                return;
            }
        }

        // =====================================================
        // ITEM BERBEDA → TUKAR
        // =====================================================

        if (to.HasItem())
        {
            itemSo tempItem =
                to.GetItem();

            int tempAmount =
                to.GetAmount();

            to.SetItem(
                from.GetItem(),
                from.GetAmount()
            );

            from.SetItem(
                tempItem,
                tempAmount
            );

            EquipHandItem();
            return;
        }

        // =====================================================
        // SLOT KOSONG
        // =====================================================

        to.SetItem(
            from.GetItem(),
            from.GetAmount()
        );

        from.ClearSlot();

        EquipHandItem();
    }

    private void UpdateDragItemPosition()
    {
        if (isDraggin && dragIcon != null)
        {
            dragIcon.transform.position =
                Input.mousePosition;
        }
    }

    // =========================================================
    // PICKUP
    // =========================================================

    private void Pickup()
    {
        // Kalau inventory terbuka, jangan pickup
        if (IsInventoryOpen)
            return;

        if (lookedAtRenderer != null &&
            Input.GetKeyDown(KeyCode.E))
        {
            Item item =
                lookedAtRenderer.GetComponent<Item>();

            if (item != null)
            {
                AddItem(
                    item.item,
                    item.amount
                );

                Destroy(item.gameObject);

                EquipHandItem();
            }
        }
    }

    // =========================================================
    // DETECT ITEM YANG DILIHAT
    // =========================================================

    private void DetectLookedAtItem()
    {
        // Kembalikan material item sebelumnya
        if (lookedAtRenderer != null)
        {
            lookedAtRenderer.material =
                originalMaterial;

            lookedAtRenderer = null;
            originalMaterial = null;
            lookedAtItem = null;
        }

        // Jangan highlight item ketika inventory terbuka
        if (IsInventoryOpen)
            return;

        if (Camera.main == null)
            return;

        Ray ray = new Ray(
            Camera.main.transform.position,
            Camera.main.transform.forward
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            pickupRange
        ))
        {
            Item item =
                hit.collider.GetComponent<Item>();

            if (item != null)
            {
                Renderer rend =
                    item.GetComponent<Renderer>();

                if (rend != null)
                {
                    originalMaterial =
                        rend.material;

                    if (highlightMaterial != null)
                    {
                        rend.material =
                            highlightMaterial;
                    }

                    lookedAtRenderer = rend;
                    lookedAtItem = item;
                }
            }
        }
    }

    // =========================================================
    // HOTBAR OPACITY
    // =========================================================

    private void UpdateHotbarOpacity()
    {
        for (int i = 0;
             i < hotbarSlots.Count;
             i++)
        {
            Image icon =
                hotbarSlots[i].GetComponent<Image>();

            if (icon != null)
            {
                icon.color =
                    (i == equippedHotBarIndex)
                    ? new Color(
                        1f,
                        1f,
                        1f,
                        equippedOpacity
                    )
                    : new Color(
                        1f,
                        1f,
                        1f,
                        normalOpacity
                    );
            }
        }
    }

    // =========================================================
    // HOTBAR SELECTION
    // =========================================================

    private void HandleHotBarSelection()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Input.GetKeyDown(
                (i + 1).ToString()
            ))
            {
                if (i >= hotbarSlots.Count)
                    continue;

                equippedHotBarIndex = i;

                UpdateHotbarOpacity();
                EquipHandItem();
            }
        }
    }

    // =========================================================
    // DROP ITEM DENGAN Q
    // =========================================================

    private void HandleDropEquippedItem()
    {
        // Jangan drop ketika inventory terbuka
        if (IsInventoryOpen)
            return;

        if (!Input.GetKeyDown(KeyCode.Q))
            return;

        if (equippedHotBarIndex < 0 ||
            equippedHotBarIndex >= hotbarSlots.Count)
        {
            return;
        }

        Slot equippedSlot =
            hotbarSlots[equippedHotBarIndex];

        if (!equippedSlot.HasItem())
            return;

        itemSo itemSO =
            equippedSlot.GetItem();

        if (itemSO == null)
            return;

        GameObject prefab =
            itemSO.itemPrefab;

        if (prefab == null)
        {
            Debug.LogWarning(
                "ItemPrefab tidak ditemukan untuk "
                + itemSO.itemName
            );

            return;
        }

        if (Camera.main == null)
            return;

        Vector3 spawnPosition =
            Camera.main.transform.position +
            Camera.main.transform.forward;

        GameObject dropped =
            Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity
            );

        Item item =
            dropped.GetComponent<Item>();

        if (item != null)
        {
            item.item = itemSO;
            item.amount =
                equippedSlot.GetAmount();
        }

        equippedSlot.ClearSlot();

        EquipHandItem();
    }

    // =========================================================
    // EQUIP HAND ITEM
    // =========================================================

    private void EquipHandItem()
    {
        // Hapus item lama di tangan
        if (currentHandItem != null)
        {
            Destroy(currentHandItem);
            currentHandItem = null;
        }

        if (equippedHotBarIndex < 0 ||
            equippedHotBarIndex >= hotbarSlots.Count)
        {
            return;
        }

        Slot equippedSlot =
            hotbarSlots[equippedHotBarIndex];

        if (!equippedSlot.HasItem())
            return;

        itemSo item =
            equippedSlot.GetItem();

        if (item == null)
            return;

        if (item.handItemPrefab == null)
            return;

        if (hand == null)
        {
            Debug.LogWarning(
                "Transform Hand belum diisi di Inventory."
            );

            return;
        }

        currentHandItem =
            Instantiate(
                item.handItemPrefab,
                hand
            );

        currentHandItem.transform.localPosition =
            Vector3.zero;

        currentHandItem.transform.localRotation =
            Quaternion.identity;
    }

    // =========================================================
    // DIGUNAKAN OLEH SHAPE PLACEMENT SYSTEM
    // =========================================================

    public itemSo GetEquippedItem()
    {
        if (equippedHotBarIndex < 0 ||
            equippedHotBarIndex >= hotbarSlots.Count)
        {
            return null;
        }

        Slot slot =
            hotbarSlots[equippedHotBarIndex];

        if (!slot.HasItem())
            return null;

        return slot.GetItem();
    }

    public int GetEquippedAmount()
    {
        if (equippedHotBarIndex < 0 ||
            equippedHotBarIndex >= hotbarSlots.Count)
        {
            return 0;
        }

        Slot slot =
            hotbarSlots[equippedHotBarIndex];

        if (!slot.HasItem())
            return 0;

        return slot.GetAmount();
    }

    public bool RemoveEquippedItem(int amount)
    {
        if (amount <= 0)
            return false;

        if (equippedHotBarIndex < 0 ||
            equippedHotBarIndex >= hotbarSlots.Count)
        {
            return false;
        }

        Slot slot =
            hotbarSlots[equippedHotBarIndex];

        if (!slot.HasItem())
            return false;

        int currentAmount =
            slot.GetAmount();

        if (currentAmount < amount)
            return false;

        currentAmount -= amount;

        if (currentAmount <= 0)
        {
            slot.ClearSlot();
        }
        else
        {
            slot.SetItem(
                slot.GetItem(),
                currentAmount
            );
        }

        // Update object yang terlihat di tangan
        EquipHandItem();

        return true;
    }

    public void BindToPlayer(Transform playerHand)
{
    if (playerHand == null)
    {
        Debug.LogWarning("Hand Point Player belum diatur!");
        return;
    }

    hand = playerHand;

    // Perbarui item visual di tangan pemain lokal.
    EquipHandItem();

    Debug.Log("Inventory berhasil terhubung ke Player lokal.");
}
}