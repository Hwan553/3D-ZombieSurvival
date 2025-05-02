 using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ZombieController : MonoBehaviour
{
    public enum ZombieState { Chase, Attack, Dead }
    private ZombieState currentState;

    public float attackRange = 2f;
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;

    private float enemyMaxHP = 10f;
    public float enemyCurrentHP = 0f;

    [SerializeField] private Slider ZombieHPBar;
    [SerializeField] private Collider RighthandCollider;

    private bool isAttacking = false;

    private AudioSource audioSource;
    public AudioClip attackSFX;
    public AudioClip deadSFX;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Rigidbody의 isKinematic 속성을 true로 설정하여 물리 엔진에서 처리되지 않도록 설정
        GetComponent<Rigidbody>().isKinematic = true;

        // 에이전트의 회피 우선 순위를 랜덤 값으로 설정
        agent.avoidancePriority = Random.Range(30, 60);

        //장애물 회피 방식을 고품질 방식으로 설정
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // 에이전트의 이동 반경 설정
        agent.radius = 0.3f;

        InitEnemyHP();
        SetState(ZombieState.Chase);
    }

    void Update()
    {
        if (enemyCurrentHP <= 0)
        {
            SetState(ZombieState.Dead);
            return;
        }

        if (ZombieHPBar != null)
            ZombieHPBar.value = enemyCurrentHP / enemyMaxHP;

        ZombieStateInfo();
    }

    public void TakeDamage(int damage)
    {
        enemyCurrentHP -= damage;

        if (enemyCurrentHP <= 0)
        {
            SetState(ZombieState.Dead);
            GameManager.instance.OnZombieKilled();
        }
    }

    // 좀비 상태 처리 함수
    void ZombieStateInfo()
    {
        // 좀비가 죽은 상태이거나 플레이어가 없으면 함수 종료
        if (currentState == ZombieState.Dead || player == null) return;

        // 현재 좀비와 플레이어 간의 거리 계산
        float distance = Vector3.Distance(transform.position, player.position);

        // 좀비 상태가 추적 상태일때
        switch (currentState)
        {
            case ZombieState.Chase:
                // 플레이어와의 거리가 공격 범위 내에 있으면 공격 상태로 전환
                if (distance <= attackRange)
                    SetState(ZombieState.Attack);
                else
                {
                    // 공격 범위 외일 경우, 플레이어를 추적
                    agent.isStopped = false;
                    agent.SetDestination(player.position); // 플레이어 위치로 목적지 설정
                    RotateToPlayer(); // 플레이어를 바라보도록 회전
                }
                break;
            // 좀비 상태가 공격 상태일 때
            case ZombieState.Attack:
                // 플레이어와의 거리가 공격 범위를 벗어나면 추적 상태로 전환
                if (distance > attackRange)
                    SetState(ZombieState.Chase);
                // 공격 중이 아니라면 공격 시작
                else if (!isAttacking)
                    StartCoroutine(AttackPlayer());
                break;
        }
    }

    // 좀비의 상태를 설정하는 함수
    void SetState(ZombieState newState)
    {
        // 좀비가 죽은 상태일 때는 상태 전환이 안 되도록 처리
        if (currentState == ZombieState.Dead) return;
        currentState = newState; // 새로운 상태로 설정

        switch (newState) // 새로운 상태에 따라 처리
        {
            // 추적 상태
            case ZombieState.Chase:
                agent.isStopped = false; // 추적을 멈추지 않도록 설정
                agent.stoppingDistance = attackRange; // 공격 범위 내에 들어오면 멈춤
                animator.SetBool("isMove", true); // 이동 애니메이션 활성화
                animator.SetBool("isAttack", false); // 공격 애니메이션 비활성화
                break;

                // 공격 상태
            case ZombieState.Attack:
                agent.isStopped = true; // 이동을 멈춤
                animator.SetBool("isMove", false); // 이동 애니메이션 비활성화
                animator.SetBool("isAttack", true); // 공격 애니메이션 활성화

                // 공격 사운드가 설정되어 있고 이미 재생 중이 아니라면 공격 사운드 재생
                if (attackSFX != null && !audioSource.isPlaying)
                    audioSource.PlayOneShot(attackSFX);
                break;

                // 죽은 상태
            case ZombieState.Dead:
                StartCoroutine(ZombieDie()); // 좀비 사망 애니메이션 및 처리를 위해 코루틴 실행
                break;
        }
    }

    IEnumerator ZombieDie()
    {
        currentState = ZombieState.Dead;

        animator.SetTrigger("isDead");

       
        if (audioSource != null)
        {
            audioSource.Stop();
            if (deadSFX != null)
                audioSource.PlayOneShot(deadSFX);
        }

        if (ZombieHPBar != null)
            ZombieHPBar.gameObject.SetActive(false);

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        enabled = false;

        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
    }

    IEnumerator AttackPlayer()
    {
        if (currentState == ZombieState.Dead) yield break;

        isAttacking = true;
        animator.SetBool("isAttack", true);
        agent.isStopped = true;

        EnableHandColliders();
        yield return new WaitForSeconds(0.5f);
        DisableHandColliders();

        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
        animator.SetBool("isAttack", false);

        if (currentState != ZombieState.Dead)
            SetState(ZombieState.Chase);
    }

    void RotateToPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    public void EnableHandColliders()
    {
        if (RighthandCollider != null) RighthandCollider.enabled = true;
    }

    public void DisableHandColliders()
    {
        if (RighthandCollider != null) RighthandCollider.enabled = false;
    }

    public void InitEnemyHP()
    {
        enemyCurrentHP = enemyMaxHP;
        gameObject.SetActive(true);

        if (ZombieHPBar != null)
        {
            ZombieHPBar.gameObject.SetActive(true);
            ZombieHPBar.value = 1f;
        }

        currentState = ZombieState.Chase;
        enabled = true;
    }
}
