using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShapeObject : MonoBehaviour
{
    [Header("Shape")]
    public ShapeType shapeType;

    private Rigidbody rb;
    private Collider[] colliders;

    public bool IsHeld { get; private set; }
    public bool IsPlaced { get; private set; }

    public bool CanBePickedUp => !IsHeld && !IsPlaced;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void PickUp(Transform holdPoint)
    {
        IsHeld = true;

        rb.isKinematic = true;
        rb.useGravity = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Throw(Vector3 direction, float force)
    {
        IsHeld = false;

        transform.SetParent(null);

        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(direction.normalized * force, ForceMode.Impulse);
    }

    public void SnapTo(Transform snapPoint)
    {
        IsHeld = false;
        IsPlaced = true;

        transform.SetParent(snapPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }
}