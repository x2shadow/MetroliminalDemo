using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

[RequireComponent(typeof(NavMeshAgent))]
public class EntityAI : MonoBehaviour
{
    public enum State { Patrol, Alerting, Chase }

    [Header("Debug / tuning")]
    [SerializeField, Tooltip("Текущее накопленное значение обнаружения (видно в инспекторе)")]
    private float detectionValue = 0f;
    public State currentState = State.Patrol;

    [Header("References")]
    [Tooltip("Player transform (auto-find by tag Player if empty)")]
    public Transform player;
    [Tooltip("PlayerStealth component (auto-find on player if empty)")]
    public PlayerStealth playerStealth;

    [Header("Patrol")]
    [Tooltip("Пункты маршрута (по очереди). Если пусто — сущность будет стоять на месте)")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float patrolSpeed = 1.5f;
    [Tooltip("Интервал рывка в режиме патруля (сек) - будет рандомизироваться между min/max")]
    public float patrolDashIntervalMin = 10f;
    public float patrolDashIntervalMax = 15f;
    public float patrolDashDistance = 1f;

    [Header("Chase")]
    public float chaseSpeed = 1.5f;
    [Tooltip("Интервал рывка в режиме погони (сек)")]
    public float chaseDashInterval = 2f;
    [Tooltip("Максимальная дистанция рывка при погоне (м)")]
    public float chaseDashDistance = 3f;
    public float attackDistance = 1f;

    [Header("Chase end wait")]
    [Tooltip("Секунд ожидать перед возвратом к патрулю после завершения погони")]
    public float chaseEndWaitSeconds = 2f;
    public bool isWaitingAfterChase = false;

    [Header("Vision")]
    public float visionRange = 8f;
    [Range(0f, 180f)] public float visionFov = 60f;
    [Tooltip("Уровень глаза (см выше центра transform) для Raycast")]
    public float eyeHeight = 1.2f;
    //[Tooltip("Слои, которые считаются препятствиями (стены и т.п.)")]
    //public LayerMask obstacleMask;

    [Header("Hearing")]
    [Tooltip("Радиус, в котором сущность слышит шумы")]
    public float hearingRadius = 4f;
    [Tooltip("Уровень шума игрока >= этого значения вызывает немедленную погони")]
    public int hearingNoiseThreshold = 2;

    [Header("Detection accumulation")]
    [Tooltip("Значение, при достижении которого начинается погоня")]
    public float detectionThreshold = 100f;
    public float maxDetectionValue = 150f;
    [Tooltip("Базовое приращение detectionValue в сек. при видимости игрока")]
    public float baseDetectionPerSecond = 25f;
    [Tooltip("Скорость убывания detectionValue в сек. когда не видно")]
    public float detectionDecayPerSecond = 30f;
    [Tooltip("Секунд, после которых когда не видно игрока — возвращаемся к патрулю")]
    public float loseSightToPatrolTime = 6f;

    [Header("Stealth modifiers")]
    [Tooltip("Множители влияния шума на скорость накопления")]
    public float runNoiseMultiplier = 1.5f;
    public float walkNoiseMultiplier = 1.0f;
    public float crouchNoiseMultiplier = 0.4f;
    public float silentNoiseMultiplier = 0f;
    [Tooltip("Multiplier when player is in dim (darknessLevel==1)")]
    public float stealthLevel1Multiplier = 0.5f;
    [Tooltip("Multiplier when player is fully dark (darknessLevel==2) - typically 0")]
    public float stealthLevel2Multiplier = 0f;

    [Header("Tuning: turning speeds (degrees/sec)")]
    [Tooltip("Скорость поворота в состоянии Alerting (градусов/сек)")]
    public float alertTurnSpeed = 120f;
    [Tooltip("Скорость поворота в состоянии Chase (градусов/сек) — высокий чтобы 'чётко' смотреть на игрока")]
    public float chaseTurnSpeed = 720f;

    [Header("Stealth modifiers")]
    public bool loadScene = true;

    [Header("Animator settings")]
    Animator animator;
    [Tooltip("Имя триггера для idle в Animator (должен совпадать c параметром в контроллере)")]
    public string idleTriggerName = "Idle";
    [Tooltip("Имя boolean-параметра Walk")]
    public string walkBoolName = "IsWalking";
    [Tooltip("Имя триггера для Dash")]
    public string dashTriggerName = "Dash";
    public string deathTriggerName = "Death";

    // Поля для смены материалов (glitch во время дэша)
    [Header("Dash VFX - material swap")]
    public Material defaultMaterial;
    [Tooltip("Material, который будет применён к объекту 'NoName' на время дэша")]
    public Material glitchMaterial;

    [Header("Death Cutscene")]
    [SerializeField] PlayableDirector deathCutscene;

    private SkinnedMeshRenderer noNameRenderer;
    private bool materialsSwapped = false;

    // internals
    [HideInInspector]
    public NavMeshAgent agent;
    private int patrolIndex = 0;
    private float lastSeenTime = -999f;
    private Coroutine dashCoroutine;
    private float nextPatrolDashTime = 0f;
    private float nextChaseDashTime = 0f;
    private bool isWaitingAtPatrol = false;
    private Coroutine patrolWaitCoroutine = null;
    private Coroutine chaseEndCoroutine = null;
    private bool isPlayerCaught = false;
    private bool isInDeathCutscene = false;

    // animator hashes
    private int hashWalk;
    private int hashDash;
    private int hashIdle;
    private int hashDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.updateUpAxis = true;

        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }

        if (playerStealth == null && player != null)
            playerStealth = player.GetComponent<PlayerStealth>();

        animator = GetComponentInChildren<Animator>();

        // prepare hashes (check for empty names to avoid errors)
        hashWalk = string.IsNullOrEmpty(walkBoolName) ? 0 : Animator.StringToHash(walkBoolName);
        hashDash = string.IsNullOrEmpty(dashTriggerName) ? 0 : Animator.StringToHash(dashTriggerName);
        hashIdle = string.IsNullOrEmpty(idleTriggerName) ? 0 : Animator.StringToHash(idleTriggerName);
        hashDeath = string.IsNullOrEmpty(deathTriggerName) ? 0 : Animator.StringToHash(deathTriggerName);

        // --- Найти SkinnedMeshRenderer дочернего объекта с именем "NoName"
        noNameRenderer = GameObject.Find("NoName").GetComponent<SkinnedMeshRenderer>();
        
        if (deathCutscene != null) { deathCutscene.stopped += OnDeathCutsceneStopped; }
    }

    private void Start()
    {
        agent.speed = patrolSpeed;
        if (patrolPoints != null && patrolPoints.Count > 0)
            MoveToNextPatrolPoint();

        ScheduleNextPatrolDash();

        // initial animation state: idle or walk depending on patrol
        if (animator != null)
        {
            SetWalking(true);
            if (hashIdle != 0) animator.ResetTrigger(hashIdle);
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                PatrolUpdate();
                break;
            case State.Alerting:
                AlertingUpdate();
                break;
            case State.Chase:
                ChaseUpdate();
                break;
        }

        // Check hearing every frame (cheap)
        CheckHearing();

        // Check vision every frame (could be optimized)
        CheckVision();
    }

    #region State updates
    private void PatrolUpdate()
    {
        if (agent.enabled == false) return;

        agent.speed = patrolSpeed;
        agent.updateRotation = true; // NavMeshAgent сам поворачивает по движению

        if (!agent.pathPending && !isWaitingAtPatrol && patrolPoints.Count > 0 && agent.remainingDistance < 0.35f)
        {
            // we arrived at patrolPoints[patrolIndex]
            Transform currentT = patrolPoints[patrolIndex];
            PatrolPoint patrolPoint = (currentT != null) ? currentT.GetComponent<PatrolPoint>() : null;

            if (patrolPoint != null && patrolPoint.type == PatrolPoint.PointType.Wait)
            {

                float wait = patrolPoint.GetWaitSeconds();
                if (patrolWaitCoroutine != null) StopCoroutine(patrolWaitCoroutine);
                patrolWaitCoroutine = StartCoroutine(WaitAtPatrolPoint(wait));
                return;
            }
            else
            {
                // no wait: advance to next
                patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
                MoveToNextPatrolPoint();
            }
        }

        // не дэшить, пока мы ждём на патрульной точке
        if (!isWaitingAtPatrol && Time.time >= nextPatrolDashTime)
        {
            StartDash(patrolDashDistance);
            ScheduleNextPatrolDash();
        }

        bool shouldWalk = agent.hasPath && !agent.isStopped && agent.remainingDistance > 0.35f;
        SetWalking(shouldWalk);
    }

    private IEnumerator WaitAtPatrolPoint(float seconds)
    {
        isWaitingAtPatrol = true;
        agent.isStopped = true;

        RestoreOriginalMaterial();

        // если дэш сейчас выполняется — остановим его
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        // Optional: здесь можно вызвать анимацию ожидания или события
        // go to idle
        if (hashIdle != 0) animator.SetTrigger(hashIdle);
        SetWalking(false);

        yield return new WaitForSeconds(seconds);

        agent.isStopped = false;
        isWaitingAtPatrol = false;

        // перенастроим время следующего патрульного дэша, чтобы он не случился мгновенно
        ScheduleNextPatrolDash();

        // advance and move to next
        SetWalking(true);
        patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
        MoveToNextPatrolPoint();
    }

    private void AlertingUpdate()
    {
        // если долго не видел — откат к патрулю
        if (Time.time - lastSeenTime > loseSightToPatrolTime)
        {
            detectionValue = 0f;
            SetState(State.Patrol);
            return;
        }

        if (TryCatchPlayer()) return;

        if (detectionValue >= detectionThreshold)
        {
            SetState(State.Chase);
            return;
        }

        // В Alerting: НЕ двигаться, НО поворачиваться к игроку
        agent.isStopped = true;
        RotateTowardsPlayer(alertTurnSpeed);

        SetWalking(false);
        if (hashIdle != 0) animator.SetTrigger(hashIdle);
    }

    private void ChaseUpdate()
    {
        if (agent.enabled == false) return;

        agent.speed = chaseSpeed;

        // Ensure agent moves, but we'll control rotation manually for precise facing
        agent.isStopped = false;
        agent.updateRotation = false; // отключаем автоматический поворот агента
    
        if (player != null) agent.SetDestination(player.position);


        if (Time.time >= nextChaseDashTime)
        {
            //StartDashTowards(player.position, chaseDashDistance); // Закоментил потому что дэшиться в стены
            StartDash(chaseDashDistance);
            nextChaseDashTime = Time.time + chaseDashInterval;
        }

        // В Chase: поворачиваемся чётче/быстрее к игроку (во время движения)
        RotateTowardsPlayer(chaseTurnSpeed);

        // lose sight and decay detectionValue
        if (Time.time - lastSeenTime > loseSightToPatrolTime)
        {
            detectionValue = Mathf.Max(0f, detectionValue - detectionDecayPerSecond * Time.deltaTime);

            if (detectionValue <= 0f)
            {
                // если ещё не запущено ожидание — стартуем его и останавливаем агента
                if (chaseEndCoroutine == null)
                {
                    // остановим движение и очистим путь
                    agent.isStopped = true;
                    isWaitingAfterChase = true;

                    SetWalking(false);
                    if (hashIdle != 0) animator.SetTrigger(hashIdle);

                    chaseEndCoroutine = StartCoroutine(WaitThenReturnToPatrol(chaseEndWaitSeconds));
                }
            }
            else
            {
                // если значение снова выросло — отменим ожидание и возобновим погоню
                if (chaseEndCoroutine != null)
                {
                    StopCoroutine(chaseEndCoroutine);
                    chaseEndCoroutine = null;
                    isWaitingAfterChase = false;
                    // восстановим движение (OnEnterChase вызовется при SetState, но если мы остались в Chase — явно вернуть движение)
                    agent.isStopped = false;
                }
            }
        }

        // while chasing and moving, play Walk
        SetWalking(!agent.isStopped);

        if (TryCatchPlayer()) return;
    }
    #endregion

    private bool TryCatchPlayer()
    {
        // compute distance to player using closest point on collider if available
        Collider playerCol = player.GetComponentInChildren<Collider>();
        Vector3 closestPoint = (playerCol != null) ? playerCol.ClosestPoint(transform.position) : player.position;
        float distToPlayer = Vector3.Distance(transform.position, closestPoint);

        if (distToPlayer <= attackDistance)
        {
            OnPlayerCaught();
            return true;
        }

        return false;
    }

    private void SetState(State newState)
    {
        if (currentState == newState) return;

        // отменим запланированный возврат к патрулю (если есть)
        if (chaseEndCoroutine != null)
        {
            StopCoroutine(chaseEndCoroutine);
            chaseEndCoroutine = null;
        }
        isWaitingAfterChase = false;

        currentState = newState;

        // state hooks for VFX/SFX
        switch (newState)
        {
            case State.Patrol:
                OnEnterPatrol();
                break;
            case State.Alerting:
                OnEnterAlerting();
                break;
            case State.Chase:
                OnEnterChase();
                ScheduleNextChaseDash();
                break;
        }
    }

    private void ScheduleNextPatrolDash()
    {
        nextPatrolDashTime = Time.time + Random.Range(patrolDashIntervalMin, patrolDashIntervalMax);
    }

    private void ScheduleNextChaseDash()
    {
        nextChaseDashTime = Time.time + chaseDashInterval;
    }

    #region Vision & Hearing
    private void CheckHearing()
    {
        if (player == null || playerStealth == null) return;

        int noise = (int)playerStealth.CurrentNoise;
        if (noise >= hearingNoiseThreshold)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= hearingRadius)
            {
                // immediate chase
                detectionValue = detectionThreshold;
                lastSeenTime = Time.time;
                SetState(State.Chase);
                OnHeardNoise();
            }
        }
    }

    private void CheckVision()
    {
        if (player == null || playerStealth == null) return;

        // if player fully hidden => no visual detection
        if (playerStealth.DarknessLevel >= 2)
            return;

        // eye and target points
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerTarget = player.position;
        Collider playerCol = player.GetComponentInChildren<Collider>();
        if (playerCol != null) playerTarget = playerCol.bounds.center;

        Vector3 toPlayer = playerTarget - eyePos;
        float dist = toPlayer.magnitude;
        //Debug.DrawRay(eyePos, toPlayer, Color.red);
        if (dist > visionRange) return;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > visionFov * 0.5f) return;

        Ray ray = new Ray(eyePos, toPlayer.normalized);

        // Single Raycast (ignore triggers). If it hits player (or child) -> visible.
        // If it hits something else before reaching the player -> blocked.
        bool visible = false;

        if (Physics.Raycast(ray, out RaycastHit hit, visionRange))
        {
            // If the first non-trigger hit is the player (or its child) -> visible
            if (hit.collider != null && (hit.collider.transform == player || hit.collider.transform.IsChildOf(player)))
            {
                visible = true;
            }
        }
        else
        {
            // no hit at all -> nothing blocks the ray -> visible
            visible = true;
        }

        // detection logic
        if (visible)
        {
            Debug.DrawRay(eyePos, toPlayer, Color.green);
            float noiseMult = GetNoiseMultiplier(playerStealth.CurrentNoise);
            float stealthMult = (playerStealth.DarknessLevel == 1) ? stealthLevel1Multiplier : 1f;
            float grow = baseDetectionPerSecond * (1f + noiseMult) * stealthMult * Time.deltaTime;
            detectionValue += grow;
            lastSeenTime = Time.time;
            detectionValue = Mathf.Clamp(detectionValue, 0f, maxDetectionValue);

            if (detectionValue >= detectionThreshold)
                SetState(State.Chase);
            else if (currentState != State.Alerting && currentState != State.Chase)
                SetState(State.Alerting);
        }
        else
        {
            if (currentState != State.Patrol)
                detectionValue = Mathf.Max(0f, detectionValue - detectionDecayPerSecond * Time.deltaTime);
        }
    }

    private float GetNoiseMultiplier(PlayerStealth.MovementNoise noise)
    {
        switch (noise)
        {
            case PlayerStealth.MovementNoise.Run: return runNoiseMultiplier;
            case PlayerStealth.MovementNoise.Walk: return walkNoiseMultiplier;
            case PlayerStealth.MovementNoise.Crouch: return crouchNoiseMultiplier;
            case PlayerStealth.MovementNoise.Silent: return silentNoiseMultiplier;
            default: return 0f;
        }
    }
    #endregion

    #region Dash helpers
    // Start forward dash (used in patrol)
    private void StartDash(float distance)
    {
        if (isPlayerCaught || isInDeathCutscene) return;
        animator.SetTrigger(hashDash);
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashForward(distance, 0.18f));
    }

    // Dash towards a target position (used in chase)
    private void StartDashTowards(Vector3 targetPos, float maxDistance)
    {
        if (isPlayerCaught || isInDeathCutscene) return;
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        Vector3 dir = (targetPos - transform.position).normalized;
        Vector3 desired = transform.position + dir * maxDistance;
        // sample navmesh so we don't dash into void
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            dashCoroutine = StartCoroutine(DashTo(hit.position, 0.20f));
        }
    }

    private IEnumerator DashForward(float distance, float duration)
    {
        Vector3 rawTarget = transform.position + transform.forward * distance;
        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            yield return DashTo(hit.position, duration);
        }
        yield break;
    }

    private IEnumerator DashTo(Vector3 to, float duration)
    {
        agent.isStopped = true;
        Vector3 from = transform.position;
        OnDashStart();
        float t = 0f;
        while (t < duration)
        {
            if (isInDeathCutscene) { break; }
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        if (!isInDeathCutscene) transform.position = to;
        OnDashEnd();
        agent.isStopped = false;

        // refresh path depending on state
        if (currentState == State.Chase && player != null)
        {
            SetWalking(true);
            agent.SetDestination(player.position);
        }
        else if (currentState == State.Patrol)
        {
            SetWalking(true);
            MoveToNextPatrolPoint();
        }
    }
    #endregion

    #region Patrol helpers
    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }
    #endregion

    #region Public helper for external noise emitters
    /// <summary>
    /// Called by external objects (NoiseEmitter, throwable items, etc).
    /// noiseLevel: 0..3, sourcePosition - world position of noise origin.
    /// </summary>
    public void TriggerHeardNoise(int noiseLevel, Vector3 sourcePosition)
    {
        if (isInDeathCutscene) return;

        float dist = Vector3.Distance(transform.position, sourcePosition);
        if (dist <= hearingRadius && noiseLevel >= hearingNoiseThreshold)
        {
            detectionValue = detectionThreshold;
            lastSeenTime = Time.time;
            SetState(State.Chase);
            OnHeardNoise();
        }
    }
    #endregion

    #region Hooks (override or assign in inspector via other script)
    protected virtual void OnEnterPatrol()
    {
        if (agent.enabled == false) return;
        // restore NavMesh automatic rotation for patrol so it faces movement
        agent.updateRotation = true;
        agent.isStopped = false;
        /* stop chase VFX/SFX */
        SetWalking(true);
        if (hashIdle != 0) animator.ResetTrigger(hashIdle);
    }
    protected virtual void OnEnterAlerting()
    {
        // если ждём на patrol point — прерываем ожидание
        /*if (patrolWaitCoroutine != null)
        {
            StopCoroutine(patrolWaitCoroutine);
            patrolWaitCoroutine = null;
        }*/

        /* subtle alert VFX */
        isWaitingAtPatrol = false;

        // В Alerting: не двигаться, но вручную поворачиваться к игроку
        agent.isStopped = true;
        //agent.ResetPath();
        agent.updateRotation = false;

        SetWalking(false);
        if (hashIdle != 0) animator.SetTrigger(hashIdle);
    }
    protected virtual void OnEnterChase()
    {
        // если ждём на patrol point — прерываем ожидание
        if (patrolWaitCoroutine != null)
        {
            StopCoroutine(patrolWaitCoroutine);
            patrolWaitCoroutine = null;
        }

        /* start chase VFX/SFX */
        isWaitingAtPatrol = false;

        // В погоне: позволяем двигаться, ручной поворот (чтобы смотреть прямо на игрока)
        agent.isStopped = false;
        agent.updateRotation = false;

        if (chaseEndCoroutine != null)
        {
            StopCoroutine(chaseEndCoroutine);
            chaseEndCoroutine = null;
            isWaitingAfterChase = false;
        }

         // Убедимся, что триггер Idle не помешает включению Walk
        if (hashIdle != 0) animator.ResetTrigger(hashIdle);

        SetWalking(true);
    }
    protected virtual void OnHeardNoise() { /* sound reaction */ }
    protected virtual void OnPlayerCaught()
    {
        if (isPlayerCaught) return; // защита от повторных вызовов
        isPlayerCaught = true;
        isInDeathCutscene = true;

        Debug.Log($"{name}: Player caught!");

        player.GetComponent<MeshRenderer>().enabled = false;
        RestoreOriginalMaterial();

        // Остановим навмеш-движение и очистим все запущенные корутины/дэши
        if (agent != null)
        {
            agent.enabled = false;
        }

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }
        if (patrolWaitCoroutine != null)
        {
            StopCoroutine(patrolWaitCoroutine);
            patrolWaitCoroutine = null;
        }
        if (chaseEndCoroutine != null)
        {
            StopCoroutine(chaseEndCoroutine);
            chaseEndCoroutine = null;
        }

        // Заблокируем дальнейшие дэши
        nextPatrolDashTime = float.MaxValue;
        nextChaseDashTime = float.MaxValue;

        // Остановим анимации ходьбы/дэша и включим death-триггер
        SetWalking(false);
        if (hashDash != 0) animator.ResetTrigger(hashDash);
        if (hashIdle != 0) animator.ResetTrigger(hashIdle);
        if (hashDeath != 0) animator.SetTrigger(hashDeath);

        // Play cutscene (если есть) — по завершении события OnDeathCutsceneStopped загрузится Level2
        if (deathCutscene != null)
        {
            deathCutscene.Play();
        }
        else
        {
            // fallback: если катсцены нет — загрузить сцену сразу
            if (loadScene) SceneManager.LoadScene("Level2");
        }
    }
    protected virtual void OnDashStart() { ApplyGlitchMaterial(); /* play glitch VFX/SFX */ }
    protected virtual void OnDashEnd() { RestoreOriginalMaterial();/* end VFX */ }
    #endregion

    private void OnDeathCutsceneStopped(PlayableDirector playableDirector)
    {
        // safety: убедимся, что этот вызов относится к нашей cutscene
        if (playableDirector != deathCutscene) return;

        // Загрузка Level2 (если разрешена)
        if (loadScene)
        {
            SceneManager.LoadScene("Level2");
        }
    }

    // helper: rotate only on Y axis towards player position with given max degrees/sec
    private void RotateTowardsPlayer(float maxDegreesPerSecond)
    {
        if (player == null) return;
        Vector3 playerTarget = player.position;
        Collider playerCol = player.GetComponentInChildren<Collider>();
        if (playerCol != null) playerTarget = playerCol.bounds.center;

        Vector3 dir = playerTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxDegreesPerSecond * Time.deltaTime);
    }

    // Coroutine that waits then returns to patrol if still not spotting player
    private IEnumerator WaitThenReturnToPatrol(float waitSeconds)
    {
        float t = 0f;
        while (t < waitSeconds)
        {
            // если состояние изменилось (например снова Chase/Alerting), отменяем
            if (currentState != State.Chase)
            {
                chaseEndCoroutine = null;
                isWaitingAfterChase = false;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // окончательно сбрасываем детекцию и возвращаемся к патрулю
        detectionValue = 0f;
        chaseEndCoroutine = null;
        isWaitingAfterChase = false;

        // перед переходом к патрулю можно проиграть анимацию начала патруля и т.д.
        SetState(State.Patrol);
    }

    // helper: set walk bool safely
    private void SetWalking(bool shouldWalk)
    {
        if (animator == null || hashWalk == 0) return;

        // Если ставим ходьбу — сбрасываем триггер Idle чтобы анимация не "залипала"
        if (shouldWalk)
        {
            if (hashIdle != 0) animator.ResetTrigger(hashIdle);
        }

        animator.SetBool(hashWalk, shouldWalk);
    }

    // --- Методы для смены/восстановления материалов
    private void ApplyGlitchMaterial()
    {
        if (noNameRenderer == null || glitchMaterial == null) return;
        if (materialsSwapped) return;

        // назначаем инстанс материалов (renderer.materials создаёт экземпляры)
        noNameRenderer.material = glitchMaterial;
        materialsSwapped = true;
    }

    private void RestoreOriginalMaterial()
    {
        if (noNameRenderer == null) return;
        if (!materialsSwapped) return;

        // восстанавливаем оригинальный массив (sharedMaterials чтобы не создавать лишние экземпляры)
        noNameRenderer.material = defaultMaterial;
        materialsSwapped = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * eyeHeight, visionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // FOV lines
        Gizmos.color = Color.white;
        Vector3 fwd = transform.forward;
        Quaternion left = Quaternion.Euler(0, -visionFov * 0.5f, 0);
        Quaternion right = Quaternion.Euler(0, visionFov * 0.5f, 0);
        Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, transform.position + Vector3.up * eyeHeight + left * fwd * visionRange);
        Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, transform.position + Vector3.up * eyeHeight + right * fwd * visionRange);
    }
}
