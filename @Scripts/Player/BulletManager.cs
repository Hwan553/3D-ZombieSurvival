using UnityEditor;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    private Rigidbody _bulletRigidbody;

    [SerializeField] private float moveSpeed = 50f;
    private float destroyTime = 3f;
    private float currentDestroyTime;


    void Awake()
    {
        _bulletRigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        currentDestroyTime = destroyTime;
    }

    void Update()
    {
        currentDestroyTime -= Time.deltaTime;

        if (currentDestroyTime <= 0f)
        {
            DestroyBullet();
        }

        BulletMove();
    }

    private void BulletMove()
    {
        _bulletRigidbody.linearVelocity = transform.forward * moveSpeed;
    }

    private void DestroyBullet()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - other.transform.position).normalized;

            ZombieController zombie = other.GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.enemyCurrentHP -= 1;
                GameManager.instance.SpawnBloodEffect(hitPoint, Quaternion.LookRotation(hitNormal));
            }

            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.enemyCurrentHP -= 1;
                GameManager.instance.SpawnBloodEffect(hitPoint, Quaternion.LookRotation(hitNormal));
            }

            DestroyBullet();
        }
    }
}

    



