using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField]
    private float _speed = 4f;

    [SerializeField]
    private float _sensitivity = 0.5f;

    private PlayerInput _controls;

    // Movement
    private Vector2 _moveVector;
    private float _elevation;

    // Look
    private Vector2 _lookVector;
    private float _xRotation = 0f;

    private Camera _playerCamera;

    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    private void Awake()
    {
        _playerCamera = GetComponentInChildren<Camera>();

        _controls = new PlayerInput();
        _controls.Player.Move.performed += ctx => _moveVector = ctx.ReadValue<Vector2>();
        _controls.Player.Move.canceled += ctx => _moveVector = Vector2.zero;

        _controls.Player.Fly.performed += ctx => _elevation = ctx.ReadValue<float>();
        _controls.Player.Fly.canceled += ctx => _elevation = 0;

        _controls.Player.Look.performed += ctx => _lookVector = ctx.ReadValue<Vector2>();
        _controls.Player.Look.canceled += ctx => _lookVector = Vector2.zero;
    }

    private void Start()
    {
        _xRotation = _playerCamera.transform.localEulerAngles.y;

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
        Vector3 move = new Vector3(_moveVector.x, _elevation, _moveVector.y) * _speed;
        transform.Translate(move * _speed * Time.deltaTime);
    }

    private void HandleLook()
    {
        float mouseX = _lookVector.x * _sensitivity;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = _lookVector.y * _sensitivity;
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        _playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }
}
