using UnityEngine;

/// <summary>
/// Hold LeftShift while carrying a card to mouse-tilt it, catching holographic light.
/// Releases back to rest position smoothly when key is released.
/// </summary>
public class InspectCardTilt : MonoBehaviour
{
    public float tiltSensitivity  = 5f;
    public float returnSmoothing  = 4f;

    private Quaternion _rest;

    private void Start() => _rest = transform.localRotation;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            float rx =  Input.GetAxis("Mouse Y") * tiltSensitivity;
            float ry = -Input.GetAxis("Mouse X") * tiltSensitivity;
            transform.Rotate(Vector3.right, rx, Space.Self);
            transform.Rotate(Vector3.up,    ry, Space.Self);
        }
        else
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, _rest, Time.deltaTime * returnSmoothing);
        }
    }
}
