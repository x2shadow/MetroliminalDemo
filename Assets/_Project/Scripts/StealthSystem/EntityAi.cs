using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

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


    // internals
    private NavMeshAgent agent;
    private int patrolIndex = 0;
    private float lastSeenTime = -999f;
    private Coroutine dashCoroutine;
    private float nextPatrolDashTime = 0f;
    private float nextChaseDashTime = 0f;

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
    }

    private void Start()
    {
        agent.speed = patrolSpeed;
        if (patrolPoints != null && patrolPoints.Count > 0)
            MoveToNextPatrolPoint();

        ScheduleNextPatrolDash();
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
        agent.speed = patrolSpeed;
        if (!agent.pathPending && agent.remainingDistance < 0.35f && patrolPoints.Count > 0)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
            MoveToNextPatrolPoint();
        }

        if (Time.time >= nextPatrolDashTime)
        {
            StartDash(patrolDashDistance);
            ScheduleNextPatrolDash();
        }
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

        if (detectionValue >= detectionThreshold)
        {
            SetState(State.Chase);
            return;
        }

        // в этом состоянии можно двигаться к последней известной позиции игрока
        if (player != null)
            agent.SetDestination(player.position);
    }

    private void ChaseUpdate()
    {
        agent.speed = chaseSpeed;
        if (player != null)
            agent.SetDestination(player.position);

        if (Time.time >= nextChaseDashTime)
        {
            //StartDashTowards(player.position, chaseDashDistance); // Закоментил потому что дэшиться в стены
            StartDash(chaseDashDistance);
            nextChaseDashTime = Time.time + chaseDashInterval;
        }

        // lose sight and decay detectionValue
        if (Time.time - lastSeenTime > loseSightToPatrolTime)
        {
            detectionValue = Mathf.Max(0f, detectionValue - detectionDecayPerSecond * Time.deltaTime);
            if (detectionValue <= 0f)
                SetState(State.Patrol);
        }

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackDistance)
        {
            OnPlayerCaught();
        }
    }
    #endregion

    private void SetState(State newState)
    {
        if (currentState == newState) return;
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

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - eyePos;
        float dist = toPlayer.magnitude;
        if (dist > visionRange) return;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > visionFov * 0.5f) return;

        Ray ray = new Ray(eyePos, toPlayer.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, visionRange))
        {
            // If the first thing hit is the player -> visible
            if (hit.collider.transform == player)
            {
                // compute growth
                float noiseMult = GetNoiseMultiplier(playerStealth.CurrentNoise);
                float stealthMult = (playerStealth.DarknessLevel == 1) ? stealthLevel1Multiplier : 1f;
                float grow = baseDetectionPerSecond * (1f + noiseMult) * stealthMult * Time.deltaTime;
                detectionValue += grow;
                lastSeenTime = Time.time;
                detectionValue = Mathf.Clamp(detectionValue, 0f, maxDetectionValue);

                if (detectionValue >= detectionThreshold)
                {
                    SetState(State.Chase);
                }
                else
                {
                    if (currentState != State.Alerting && currentState != State.Chase)
                        SetState(State.Alerting);
                }
                return;
            }
            else
            {
                // hit obstacle first -> not visible. decay if needed
                if (currentState != State.Patrol)
                    detectionValue = Mathf.Max(0f, detectionValue - detectionDecayPerSecond * Time.deltaTime);
            }
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
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashForward(distance, 0.18f));
    }

    // Dash towards a target position (used in chase)
    private void StartDashTowards(Vector3 targetPos, float maxDistance)
    {
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
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        transform.position = to;
        OnDashEnd();
        agent.isStopped = false;

        // refresh path depending on state
        if (currentState == State.Chase && player != null)
            agent.SetDestination(player.position);
        else if (currentState == State.Patrol)
            MoveToNextPatrolPoint();
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
    protected virtual void OnEnterPatrol() { /* stop chase VFX/SFX */ }
    protected virtual void OnEnterAlerting() { /* subtle alert VFX */ }
    protected virtual void OnEnterChase() { /* start chase VFX/SFX */ }
    protected virtual void OnHeardNoise() { /* sound reaction */ }
    protected virtual void OnPlayerCaught() { Debug.Log($"{name}: Player caught!"); }
    protected virtual void OnDashStart() { /* play glitch VFX/SFX */ }
    protected virtual void OnDashEnd() { /* end VFX */ }
    #endregion

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
