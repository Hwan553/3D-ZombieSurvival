using UnityEngine;

public class MissileManager : MonoBehaviour
{
    public float damage = 10f;

    private void OnEnable()
    {
        CancelInvoke();
        Invoke("DisableSelf", 5f);
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 _hitPoint = other.ClosestPoint(transform.position);
            Vector3 _hitNormal = (transform.position - other.transform.position).normalized;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.playerCurrentHP -= damage;
                player.SendMessage("DisPlayHP");
                GameManager.instance.SpawnExplosionEffect(_hitPoint, Quaternion.LookRotation(_hitNormal));

                if (player.playerCurrentHP <= 0)
                    player.SendMessage("Die");
            }

            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
