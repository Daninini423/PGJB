
using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowSystem : MonoBehaviour
{
    [Header("Throw Settings")]
    public float throwForce = 15f;
    public float upwardForce = 3f;
    public float spinForce = 5f; // Memberikan efek rotasi pada balok saat dilempar

    [Header("References")]
    public Transform playerCamera;
    public Transform holdPosition;

    public GameObject currentHeldObject;

    void Update()
    {
        // Cek apakah pemain menekan klik kiri dan sedang memegang sebuah balok
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            currentHeldObject != null)
        {
            ThrowObject();
        }
    }

    private void ThrowObject()
    {
        Rigidbody rb = currentHeldObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            currentHeldObject.transform.SetParent(null);

            Vector3 forceDirection =
                playerCamera.forward * throwForce +
                playerCamera.up * upwardForce;

            rb.AddForce(forceDirection, ForceMode.Impulse);

            Vector3 randomSpin = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            );

            rb.AddTorque(randomSpin * spinForce, ForceMode.Impulse);
        }

        currentHeldObject = null;
    }
}

