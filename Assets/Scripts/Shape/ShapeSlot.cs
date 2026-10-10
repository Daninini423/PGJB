using UnityEngine;

public class ShapeSlot : MonoBehaviour
{
    [Header("Shape yang diterima")]
    [SerializeField] private ShapeType expectedShape;

    [Header("Posisi objek setelah ditempatkan")]
    [SerializeField] private Transform snapPoint;

    [Header("Visual Highlight")]
    [SerializeField] private GameObject highlightObject;

    [Header("Warna Highlight")]
    [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.1f, 0.1f, 1f);

    private ShapeObject placedObject;

    private Renderer highlightRenderer;

    private void Awake()
    {
        if (snapPoint == null)
        {
            snapPoint = transform;
        }

        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponentInChildren<Renderer>();

            // Highlight mati saat game dimulai
            highlightObject.SetActive(false);
        }
    }

    public bool IsOccupied()
    {
        return placedObject != null;
    }

    public bool CanPlace(ShapeObject objectToPlace)
    {
        if (objectToPlace == null)
            return false;

        if (placedObject != null)
            return false;

        return objectToPlace.shapeType == expectedShape;
    }

    public void SetHighlight(int state)
    {
        if (highlightObject == null)
            return;

        // 0 = mati
        if (state == 0)
        {
            highlightObject.SetActive(false);
            return;
        }

        // 1 = hijau
        // 2 = merah
        highlightObject.SetActive(true);

        Color targetColor;

        if (state == 1)
        {
            targetColor = validColor;
        }
        else
        {
            targetColor = invalidColor;
        }

        if (highlightRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            highlightRenderer.GetPropertyBlock(block);

            Material material = highlightRenderer.sharedMaterial;

            if (material != null)
            {
                if (material.HasProperty("_BaseColor"))
                {
                    block.SetColor("_BaseColor", targetColor);
                }
                else if (material.HasProperty("_Color"))
                {
                    block.SetColor("_Color", targetColor);
                }
            }

            highlightRenderer.SetPropertyBlock(block);
        }
    }

    public void PlaceObject(ShapeObject objectToPlace)
    {
        if (!CanPlace(objectToPlace))
            return;

        placedObject = objectToPlace;

        objectToPlace.SnapTo(snapPoint);

        SetHighlight(0);
    }
}