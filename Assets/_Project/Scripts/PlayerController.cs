using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 30f;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController characterController;
    public Camera playerCamera;
    private float xRotation = 0f;


    [HideInInspector]
    public InputActions inputActions;

    [Header("Пауза")]
    [SerializeField] private GameObject pauseCanvas;
    private bool isPaused = false;

    private bool isInputBlocked = false;

    [Header("Дебаг")]
    public GameObject entityModel;
    public string idleAnimationName = "Idle";
    public string cutsceneAnimationName = "CutScene";
    public Animation entityAnimation;
    private bool isEntityVisible = true;

    [Header("Physics / Ground check")]
    public Transform groundCheck;
    public float groundDistance = 0.18f;
    [SerializeField] LayerMask groundMask;
    public float gravity = -9.81f;
    public bool isGrounded = false;

    private Vector3 velocity;

    [Header("Frame Rate Settings")]
    public FPSCounter fpsCounter;
    [Tooltip("Target frame rate (-1 for unlimited)")]
    public int targetFrameRate = 60;

    //[Tooltip("VSync count (0 = off, 1 = every VBlank, 2 = every second VBlank)")]
    //public int vSyncCount = 0;

    private void Awake()
    {
        inputActions = new InputActions();
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;
    }

    void Start()
    {
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 30f);
        mouseSensitivity = savedSensitivity;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        #if !UNITY_EDITOR
            ApplyFrameRateSettings();
        #endif
    }

    private void OnEnable()
    {
        inputActions.Enable();
        // Подписка на события для экшенов
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled  += OnMove;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled  += OnLook;
        //inputActions.Player.Click.performed += OnClick;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Pause.performed += OnPause;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        // Отписка от событий
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;
        //inputActions.Player.Click.performed -= OnClick;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Pause.performed -= OnPause;
    }

    private void Update()
    {
        if (isInputBlocked) return;

        // Движение персонажа
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Проверка земли
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        else
            isGrounded = characterController.isGrounded;

        // если на земле и идёт небольшая вниз. скорость — удерживаем на небольшом значении, чтобы персонаж "прислонялся" к земле
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // применяем гравитацию
        velocity.y += gravity * Time.deltaTime;

        // объединяем движение
        Vector3 finalMove = move * moveSpeed + velocity;

        // двигаем CharacterController
        characterController.Move(finalMove * Time.deltaTime);

        // Обработка обзора (поворот камеры)
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // Дебаг-клавиши (добавить в конец метода)
        HandleDebugKeys();
    }

    
    void OnDrawGizmos()
    {
        if (isGrounded)
        {
            Gizmos.color = Color.green;
            //Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistance);
        }
        else Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        if (context.performed) Interact();
    }

    private void Interact()
    {
        Debug.Log("Interact pressed");
    }

    private void HandleDebugKeys()
    {
        if (Keyboard.current != null && !isInputBlocked)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                if (entityAnimation != null && entityAnimation.GetClip(idleAnimationName) != null)
                {
                    entityAnimation.Play(idleAnimationName);
                    Debug.Log($"Playing idle animation: {idleAnimationName}");
                }
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                if (entityAnimation != null && entityAnimation.GetClip(cutsceneAnimationName) != null)
                {
                    entityAnimation.Play(cutsceneAnimationName);
                    Debug.Log($"Playing cutscene animation: {cutsceneAnimationName}");
                }
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                if (entityModel != null)
                {
                    isEntityVisible = !isEntityVisible;
                    entityModel.SetActive(isEntityVisible);
                    Debug.Log($"Entity visibility: {isEntityVisible}");
                }
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                fpsCounter.enabled = !fpsCounter.enabled;
            }
        }
    }

    public void ApplyFrameRateSettings()
    {
        // Установка целевого FPS
        Application.targetFrameRate = targetFrameRate;
        
        // Настройка VSync
        //QualitySettings.vSyncCount = vSyncCount;
        
        //Debug.Log($"Frame rate settings applied: Target={targetFrameRate}, VSync={vSyncCount}");
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseCanvas.SetActive(isPaused);

        if (isPaused)
        {
            Time.timeScale = 0f;  // Останавливаем время
            SetInputBlocked(true); // Блокируем управление
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;  // Возвращаем время
            SetInputBlocked(false); // Возвращаем управление
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        isInputBlocked = blocked;
        if (blocked)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
        }
    }

    public void SetInputBlocked2(bool blocked)
    {
        isInputBlocked = blocked;
    }
}
