using System.Collections;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public class BossController : MonoBehaviour
{
    public enum BossState { Chase, Attack, Dead }
    private BossState currentState;

    public float attackRange = 100f;
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;

    private float enemyMaxHP = 30f;
    public float enemyCurrentHP = 0f;

    [SerializeField] private Slider ZombieHPBar;
    public Transform missileSpawnPoint;

    private bool isAttacking = false;

    [Header("Audio")]
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip firingSFX;
    private AudioSource audioSource;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        
        GetComponent<Rigidbody>().isKinematic = true;
        agent.avoidancePriority = Random.Range(30, 60);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        agent.radius = 0.5f;

        InitEnemyHP();
        SetState(BossState.Chase);
    }

    void Update()
    {
        if (enemyCurrentHP <= 0)
        {
            SetState(BossState.Dead);
            return;
        }

        if (ZombieHPBar != null)
            ZombieHPBar.value = enemyCurrentHP / enemyMaxHP;

        BossStateInfo();
    }

    public void TakeDamage(int damage)
    {
        if (enemyCurrentHP <= 0) return;

        enemyCurrentHP -= damage;

        if (enemyCurrentHP <= 0)
        {
            SetState(BossState.Dead);
            GameManager.instance.OnZombieKilled();
        }
    }

    void BossStateInfo()
    {
        if (currentState == BossState.Dead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case BossState.Chase:
                if (distance <= attackRange)
                    SetState(BossState.Attack);
                else
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    RotateToPlayer();
                }
                break;

            case BossState.Attack:
                if (distance > attackRange)
                    SetState(BossState.Chase);
                else if (!isAttacking)
                    StartCoroutine(AttackPlayer());
                break;
        }
    }

    void SetState(BossState newState)
    {
        if (currentState == BossState.Dead) return;
        currentState = newState;

        switch (newState)
        {
            case BossState.Chase:
                agent.isStopped = false;
                agent.stoppingDistance = attackRange;
                animator.SetBool("isMove", true);
                animator.SetBool("isAttack", false);
                break;

            case BossState.Attack:
                agent.isStopped = true;
                animator.SetBool("isMove", false);
                break;

            case BossState.Dead:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                animator.SetTrigger("isDead");

                if (deathSFX != null && !audioSource.isPlaying)
                    audioSource.PlayOneShot(deathSFX);

                if (ZombieHPBar != null) ZombieHPBar.gameObject.SetActive(false);
                StartCoroutine(BossDie());
                break;
        }
    }

    IEnumerator BossDie()
    {
        yield return new WaitForSeconds(4f);
        gameObject.SetActive(false);
        enabled = false;
        GameManager.instance.GameClear();
    }

    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        animator.SetBool("isAttack", true);

        RotateToPlayer();

        yield return new WaitForSeconds(0.7f); 

        yield return new WaitForSeconds(2f);
        isAttacking = false;
        animator.SetBool("isAttack", false);

        if (currentState != BossState.Dead)
            SetState(BossState.Chase);
    }

    void LaunchMissile()
    {
        if (missileSpawnPoint == null || player == null) return;

        Vector3 dir = player.position - missileSpawnPoint.position;
        dir.y = -1f;
        Quaternion rot = Quaternion.LookRotation(dir);

        if(firingSFX != null && !audioSource.isPlaying)
            audioSource.PlayOneShot(firingSFX);

        GameManager.instance.SpawnMissile(missileSpawnPoint.position, rot);
    }

    void RotateToPlayer()
    {
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    public void InitEnemyHP()
    {
        enemyCurrentHP = enemyMaxHP;
        gameObject.SetActive(true);
    }
}
