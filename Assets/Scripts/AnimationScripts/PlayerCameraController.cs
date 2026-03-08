using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform playerBody;

    public float mouseSensitivity = 200f;
    public float thirdPersonDistance = 4f;

    [Header("Crosshair")]
    [Tooltip("Length of each crosshair arm in pixels.")]
    [SerializeField] private int crosshairArmLength = 10;
    [Tooltip("Thickness of the crosshair lines in pixels.")]
    [SerializeField] private int crosshairThickness = 2;
    [Tooltip("Gap between the center and the start of each arm.")]
    [SerializeField] private int crosshairGap = 4;
    [SerializeField] private Color crosshairColor = Color.white;

    float xRotation = 0f;
    bool isFirstPerson = false;

    // All renderers on the player body — hidden in first-person to avoid seeing your own face
    private Renderer[] _bodyRenderers;
    private Texture2D _crosshairTex;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerBody != null)
            _bodyRenderers = playerBody.GetComponentsInChildren<Renderer>();

        // Build a solid-colour 1x1 texture for the crosshair lines
        _crosshairTex = new Texture2D(1, 1);
        _crosshairTex.SetPixel(0, 0, crosshairColor);
        _crosshairTex.Apply();

        SetBodyVisible(!isFirstPerson);
    }

    void Update()
    {
        // Switch mode
        if (Input.GetKeyDown(KeyCode.V))
        {
            isFirstPerson = !isFirstPerson;
            SetBodyVisible(!isFirstPerson);
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

    void OnGUI()
    {
        if (!isFirstPerson || _crosshairTex == null) return;

        float cx = Screen.width  / 2f;
        float cy = Screen.height / 2f;
        int t  = crosshairThickness;
        int g  = crosshairGap;
        int a  = crosshairArmLength;

        // Left arm
        GUI.DrawTexture(new Rect(cx - g - a, cy - t / 2f, a, t), _crosshairTex);
        // Right arm
        GUI.DrawTexture(new Rect(cx + g,     cy - t / 2f, a, t), _crosshairTex);
        // Top arm
        GUI.DrawTexture(new Rect(cx - t / 2f, cy - g - a, t, a), _crosshairTex);
        // Bottom arm
        GUI.DrawTexture(new Rect(cx - t / 2f, cy + g,     t, a), _crosshairTex);
    }

    private void SetBodyVisible(bool visible)
    {
        if (_bodyRenderers == null) return;
        foreach (var r in _bodyRenderers)
            r.enabled = visible;
    }
}
