using UnityEngine;

/// <summary>
/// First-person player movement with mouse look.
/// Automatically pauses when a UI panel (tablet, inventory, etc.) is open.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed        = 5f;
    public float sprintMultiplier = 1.8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxVerticalAngle = 80f;
    public Transform cameraTransform;

    private CharacterController _cc;
    private float _verticalAngle;
    public static bool UIOpen { get; set; }   // set by any UI panel on open/close

    // ----------------------------------------------------------------

    private void Start()
    {
        _cc = GetComponent<CharacterController>();
        LockCursor(true);
    }

    private void Update()
    {
        if (UIOpen) return;

        HandleLook();
        HandleMove();

        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(!IsCursorLocked());
    }

    private void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);
        _verticalAngle = Mathf.Clamp(_verticalAngle - my, -maxVerticalAngle, maxVerticalAngle);
        cameraTransform.localRotation = Quaternion.Euler(_verticalAngle, 0f, 0f);
    }

    private void HandleMove()
    {
        float   speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 dir   = transform.right   * Input.GetAxis("Horizontal")
                      + transform.forward * Input.GetAxis("Vertical");
        _cc.Move(dir * speed * Time.deltaTime);
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    private static bool IsCursorLocked() => Cursor.lockState == CursorLockMode.Locked;
}
