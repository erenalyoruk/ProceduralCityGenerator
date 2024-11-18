using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float sensitivity = 0.5f;

    private PlayerInput controls;

    // Movement
    private Vector2 moveVector;
    private float elevation;

    // Look
    private Vector2 lookVector;
    private float xRotation = 0f;

    private Camera playerCamera;

    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();

        controls = new PlayerInput();
        controls.Player.Move.performed += ctx => moveVector = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveVector = Vector2.zero;

        controls.Player.Fly.performed += ctx => elevation = ctx.ReadValue<float>();
        controls.Player.Fly.canceled += ctx => elevation = 0;

        controls.Player.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookVector = Vector2.zero;
    }

    private void Start()
    {
        xRotation = playerCamera.transform.localEulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        Vector3 move = new Vector3(moveVector.x, elevation, moveVector.y) * speed;
        transform.Translate(move * speed * Time.deltaTime);
    }

    private void HandleLook()
    {
        float mouseX = lookVector.x * sensitivity;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = lookVector.y * sensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
