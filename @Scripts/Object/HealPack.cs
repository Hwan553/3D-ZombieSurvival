using UnityEngine;

public class HealPack : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.up * 30f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController _player = other.GetComponent<PlayerController>();
            if (_player != null)
            {
                _player.Heal(25);
                _player.PlayHealEffect();
                gameObject.SetActive(false);
            }
        }
    }
}
