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

        // Rigidbody�� isKinematic �Ӽ��� true�� �����Ͽ� ���� �������� ó������ �ʵ��� ����
        GetComponent<Rigidbody>().isKinematic = true;

        // ������Ʈ�� ȸ�� �켱 ������ ���� ������ ����
        agent.avoidancePriority = Random.Range(30, 60);

        //��ֹ� ȸ�� ����� ��ǰ�� ������� ����
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // ������Ʈ�� �̵� �ݰ� ����
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

    // ���� ���� ó�� �Լ�
    void ZombieStateInfo()
    {
        // ���� ���� �����̰ų� �÷��̾ ������ �Լ� ����
        if (currentState == ZombieState.Dead || player == null) return;

        // ���� ����� �÷��̾� ���� �Ÿ� ���
        float distance = Vector3.Distance(transform.position, player.position);

        // ���� ���°� ���� �����϶�
        switch (currentState)
        {
            case ZombieState.Chase:
                // �÷��̾���� �Ÿ��� ���� ���� ���� ������ ���� ���·� ��ȯ
                if (distance <= attackRange)
                    SetState(ZombieState.Attack);
                else
                {
                    // ���� ���� ���� ���, �÷��̾ ����
                    agent.isStopped = false;
                    agent.SetDestination(player.position); // �÷��̾� ��ġ�� ������ ����
                    RotateToPlayer(); // �÷��̾ �ٶ󺸵��� ȸ��
                }
                break;
            // ���� ���°� ���� ������ ��
            case ZombieState.Attack:
                // �÷��̾���� �Ÿ��� ���� ������ ����� ���� ���·� ��ȯ
                if (distance > attackRange)
                    SetState(ZombieState.Chase);
                // ���� ���� �ƴ϶�� ���� ����
                else if (!isAttacking)
                    StartCoroutine(AttackPlayer());
                break;
        }
    }

    // ������ ���¸� �����ϴ� �Լ�
    void SetState(ZombieState newState)
    {
        // ���� ���� ������ ���� ���� ��ȯ�� �� �ǵ��� ó��
        if (currentState == ZombieState.Dead) return;
        currentState = newState; // ���ο� ���·� ����

        switch (newState) // ���ο� ���¿� ���� ó��
        {
            // ���� ����
            case ZombieState.Chase:
                agent.isStopped = false; // ������ ������ �ʵ��� ����
                agent.stoppingDistance = attackRange; // ���� ���� ���� ������ ����
                animator.SetBool("isMove", true); // �̵� �ִϸ��̼� Ȱ��ȭ
                animator.SetBool("isAttack", false); // ���� �ִϸ��̼� ��Ȱ��ȭ
                break;

                // ���� ����
            case ZombieState.Attack:
                agent.isStopped = true; // �̵��� ����
                animator.SetBool("isMove", false); // �̵� �ִϸ��̼� ��Ȱ��ȭ
                animator.SetBool("isAttack", true); // ���� �ִϸ��̼� Ȱ��ȭ

                // ���� ���尡 �����Ǿ� �ְ� �̹� ��� ���� �ƴ϶�� ���� ���� ���
                if (attackSFX != null && !audioSource.isPlaying)
                    audioSource.PlayOneShot(attackSFX);
                break;

                // ���� ����
            case ZombieState.Dead:
                StartCoroutine(ZombieDie()); // ���� ��� �ִϸ��̼� �� ó���� ���� �ڷ�ƾ ����
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
