using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public static PlayerCam Instance;

   [Header("Sensitivity)")]
   public float sensX;
   public float sensY;

   public Transform oritentation;
   public Transform modelRotation;

   public bool updatingRotation;

   private float xRotation;
   private float yRotation;

   private void Awake()
   {
    Instance = this;
   }

private void Start()
   {
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    updatingRotation = true; // Set true agar kamera aktif berotasi
   }

   // Ubah 'update' menjadi 'Update' (Huruf U Kapital)
   private void Update()
   {
    if(!updatingRotation) return;
    float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
    float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;

    yRotation += mouseX;
    xRotation -= mouseY;
    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

    transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    oritentation.rotation = Quaternion.Euler(0, yRotation, 0);
    modelRotation.rotation = oritentation.rotation;
   }

}
