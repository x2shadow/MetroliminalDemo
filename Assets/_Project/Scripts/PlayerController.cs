using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 1.0f;

    [Header("Sprint / Crouch")]
    [Tooltip("Множитель скорости при беге")]
    public float sprintMultiplier = 1.7f;
    [Tooltip("Множитель скорости при приседе")]
    public float crouchSpeedMultiplier = 0.5f;
    [Tooltip("Высота CharacterController в стоячем состоянии")]
    public float standingHeight = 2.0f;
    [Tooltip("Высота CharacterController в приседе")]
    public float crouchHeight = 1.0f;
    [Tooltip("Скорость перехода между высотами")]
    public float crouchTransitionSpeed = 8f;
    [Tooltip("Слой(ы), которые блокируют возможность встать (потолок и пр.)")]
    public LayerMask ceilingMask;
    //[Tooltip("Input Action Reference для Sprint (обязательно или оставьте пустым если будете управлять состоянием извне).")]
    //public InputActionReference sprintActionReference;
    //[Tooltip("Input Action Reference для Crouch (toggle).")]
    //public InputActionReference crouchActionReference;

    [Header("Stealth integration")]
    [Tooltip("PlayerStealth component (auto-find if empty)")]
    public PlayerStealth playerStealth;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController characterController;

    [Header("Камера игрока")]
    public Camera playerCamera;
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;
    [Tooltip("Invert Y look (true = typical 'flight' invert)")]
    public bool invertY = false;

    // cinemachine
    private float cinemachineTargetPitch;
    private float rotationVelocity;
    private const float threshold = 0.01f;

    [Header("Camera Shake")]
    public PlayerCameraShake playerCameraShake;

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
    public bool isDialogueActive;

    [Header("Physics / Ground check")]
    public Transform groundCheck;
    public float groundDistance = 0.18f;
    [SerializeField] LayerMask groundMask;
    public float gravity = -9.81f;
    public bool isGrounded = false;

    private Vector3 velocity;

    [Header("Взаимодействие")]
    public float interactDistance = 3f; // дальность луча
    public LayerMask interactMask; // слои для проверки
    public LayerMask obstacleMask; // слои препятствий
    public GameObject interactPromptUI; // UI-элемент "E" в Canvas
    public DialogueRunner dialogueRunner;

    private IInteractable currentInteractable;

    [Header("Фонарик")]
    [SerializeField] private Light flashlight;

    [Header("Frame Rate Settings")]
    public FPSCounter fpsCounter;
    [Tooltip("Target frame rate (-1 for unlimited)")]
    public int targetFrameRate = 60;

    //[Tooltip("VSync count (0 = off, 1 = every VBlank, 2 = every second VBlank)")]
    //public int vSyncCount = 0;

    [HideInInspector] public bool hasKey = false;

    // Sprint/Crouch internal states
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float currentHeight;
    private Vector3 cameraInitialLocalPos;
    private Vector3 cameraCrouchLocalPos;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return inputActions.Player.Look.activeControl?.device is UnityEngine.InputSystem.Mouse;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        inputActions = new InputActions();
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;

        if (playerStealth == null) playerStealth = GetComponent<PlayerStealth>();

        originalControllerHeight = characterController.height;
        originalControllerCenter = characterController.center;
        currentHeight = characterController.height;

        cameraInitialLocalPos = CinemachineCameraTarget.transform.localPosition;
        float heightDiff = Mathf.Max(0f, standingHeight - crouchHeight);
        cameraCrouchLocalPos = cameraInitialLocalPos - new Vector3(0f, heightDiff * 0.5f, 0f);
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
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
        //inputActions.Player.Click.performed += OnClick;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Flashlight.performed += OnFlashlight;
        inputActions.Player.Pause.performed += OnPause;

        inputActions.Player.Sprint.performed += OnSprintPerfomed;
        inputActions.Player.Sprint.canceled  += OnSprintCanceled;
        inputActions.Player.Crouch.performed += OnCrouch;
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
        inputActions.Player.Flashlight.performed -= OnFlashlight;
        inputActions.Player.Pause.performed -= OnPause;
        
        inputActions.Player.Sprint.performed -= OnSprintPerfomed;
        inputActions.Player.Sprint.canceled  -= OnSprintCanceled;
        inputActions.Player.Crouch.performed -= OnCrouch;
    }

    private void Update()
    {
        if (isInputBlocked) return;

        HandleMovement();
        HandleInteractionRay();
        HandleDebugKeys();
        HandleCrouchHeightTransition();
    }

    void LateUpdate()
    {
        HandleLook();

        // HandleCameraShake
        if (playerCameraShake != null)
        {
            Vector3 horizVel = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            float maxPossibleSpeed = moveSpeed * sprintMultiplier; // можно изменить логику при желании
            playerCameraShake.UpdateShake(horizVel, maxPossibleSpeed, isSprinting, isGrounded);
        }
    }

    void HandleMovement()
    {
        // Движение персонажа
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = move.normalized; // нормализация

        // вычисляем текущую скорость с учётом Sprint/Crouch
        float speedMultiplier = 1f;
        if (isSprinting) speedMultiplier *= sprintMultiplier;
        if (isCrouching) speedMultiplier *= crouchSpeedMultiplier;
        float effectiveSpeed = moveSpeed * speedMultiplier;

        // ----------------- STEALTH: обновляем уровень шума -----------------
        if (playerStealth != null)
        {
            // Определяем шум по приоритету: crouch > sprint > moving > idle
            if (isCrouching)
            {
                playerStealth.SetMovementNoise(PlayerStealth.MovementNoise.Crouch);
            }
            else if (isSprinting && move.magnitude > 0.01f)
            {
                playerStealth.SetMovementNoise(PlayerStealth.MovementNoise.Run);
            }
            else if (move.magnitude > 0.01f)
            {
                playerStealth.SetMovementNoise(PlayerStealth.MovementNoise.Walk);
            }
            else
            {
                playerStealth.SetMovementNoise(PlayerStealth.MovementNoise.Silent);
            }
        }
        // ------------------------------------------------------------------

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
        Vector3 finalMove = move * effectiveSpeed + velocity;

        // двигаем CharacterController
        characterController.Move(finalMove * Time.deltaTime);
    }

    void HandleLook()
    {
        // if there is an input
        if (lookInput.sqrMagnitude >= threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            // обработка инверсии по Y
            float yInput = invertY ? lookInput.y : -lookInput.y;

            cinemachineTargetPitch += yInput * mouseSensitivity * deltaTimeMultiplier;
            rotationVelocity = lookInput.x * mouseSensitivity * deltaTimeMultiplier;

            // clamp our pitch rotation
            cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * rotationVelocity);
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        angle %= 360f; // нормализуем в диапазон -360...360
        if (angle < -180f) angle += 360f; // теперь диапазон -180...180
        return Mathf.Clamp(angle, min, max);
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

    private void HandleInteractionRay()
    {
        currentInteractable = null;
        interactPromptUI.SetActive(false);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask | obstacleMask))
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();

            if (currentInteractable != null)
            {
                if (currentInteractable.GetUsed()) return;
                interactPromptUI.SetActive(true);
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        if (context.performed && currentInteractable != null)
        {
            currentInteractable.Interact(this);
            //Interact();
        }
    }

    private void Interact()
    {
        Debug.Log("Interact pressed");
    }

    private void OnFlashlight(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        if (context.performed)
        {
            flashlight.enabled = !flashlight.enabled;
        }
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
    
    private void OnSprintPerfomed(InputAction.CallbackContext ctx)
    {
        if (isCrouching) return; // не бегаем в приседе
        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (!isCrouching)
        {
            // садимся
            isCrouching = true;
            isSprinting = false;
        }
        else
        {
            isCrouching = false;
            /*
            // пытаемся встать
            if (CanStandUp())
            {
                isCrouching = false;
            }
            else
            {
                // остаться в приседе
                isCrouching = true;
            }*/
        }

        // Обновляем PlayerStealth о том, что игрок присел/встал
        if (playerStealth != null)
        {
            playerStealth.SetCrouch(isCrouching);
        }
    }

    private bool CanStandUp()
    {
        float checkRadius = 0.2f;
        Vector3 checkCenter = transform.position + Vector3.up * (crouchHeight + 0.1f);
        LayerMask maskToUse = (ceilingMask != 0) ? ceilingMask : obstacleMask;
        bool blocked = Physics.CheckSphere(checkCenter, checkRadius, maskToUse);
        return !blocked;
    }

    private void HandleCrouchHeightTransition()
    {
        float desiredHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(characterController.height, desiredHeight, Time.deltaTime * crouchTransitionSpeed);

        // Вычисляем смещение центра относительно оригинального центра (чтобы не "тонуть" в землю)
        float heightDeltaFromOriginal = newHeight - originalControllerHeight;
        Vector3 newCenter = originalControllerCenter + Vector3.up * (heightDeltaFromOriginal / 2f);

        characterController.height = newHeight;
        characterController.center = newCenter;

        if (CinemachineCameraTarget != null)
        {
            Vector3 targetCamPos = isCrouching ? cameraCrouchLocalPos : cameraInitialLocalPos;
            CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(CinemachineCameraTarget.transform.localPosition, targetCamPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }
}
