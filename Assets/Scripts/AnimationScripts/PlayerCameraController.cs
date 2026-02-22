using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform playerBody;

    public float mouseSensitivity = 200f;
    public float thirdPersonDistance = 4f;

    float xRotation = 0f;
    bool isFirstPerson = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Switch mode
        if (Input.GetKeyDown(KeyCode.V))
        {
            isFirstPerson = !isFirstPerson;
        }

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        // Camera position
        if (isFirstPerson)
        {
            cameraPivot.localPosition = new Vector3(0, 1.6f, 0);
            Camera.main.transform.localPosition = Vector3.zero;
        }
        else
        {
            cameraPivot.localPosition = new Vector3(0, 1.6f, 0);
            Camera.main.transform.localPosition = new Vector3(0, 0, -thirdPersonDistance);
        }
    }
}
