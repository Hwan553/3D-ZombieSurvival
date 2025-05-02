using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UIElements;
using JetBrains.Annotations;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Bullet")]
    [SerializeField] private Transform bulletPoint;
    [SerializeField] private GameObject bulletObj;
    [SerializeField] private Text bulletText;
    private int maxBullet = 30;
    private int currentBullet = 0;

    [Header("Weapon FX")]
    [SerializeField] private GameObject weaponFlashFX;
    [SerializeField] private Transform bulletCasePoint;
    [SerializeField] private GameObject weaponCaseObj;
    [SerializeField] private Transform weaponClipPoint;
    [SerializeField] private GameObject weaponClipFX;

    [Header("Zombie")]
    [SerializeField] private GameObject[] spawnPoints;
    [SerializeField] private GameObject bossZombiePrefab;
    private int zombieKillCount = 0;
    private int zombieSpawnedCount = 0;
    private int maxZombies = 10;
    private bool bossSpawned = false;

    [Header("Heal Pack")]
    [SerializeField] private Transform[] healPackPoints;
    private List<GameObject> activeHealPacks = new List<GameObject>();
    private int healPackPoolIndex = 8;



    [Header("UI")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject gameClearUI;

    [Header("Audio")]
    [SerializeField] private AudioClip explosionSFX;
    [SerializeField] private AudioClip clearSFX;
    [SerializeField] private AudioClip gameoverSFX;
    private AudioSource audioResource;


    private void Start()
    {
        instance = this;
        StartCoroutine(ZombieSpawn());
        InitBullet();
        gameOverUI.SetActive(false);
        SpawnAllHealPacks();
        StartCoroutine(HealPackRespawnRoutine());
        Time.timeScale = 1;
        OnZombieKilled();

        audioResource = GetComponent<AudioSource>();
    }



    private void Update()
    {
        bulletText.text = currentBullet + " / " + maxBullet;
    }

    // ��� �Լ�
    public void Shooting(Vector3 targetPosition, ZombieController zombieCtr)
    {
        // ź���� ������ ��� �Ұ�
        if (currentBullet <= 0) return;

        // ź�� 1�� �Ҹ�
        currentBullet--;

        // ���� ���� ���(�ѱ� -> ��ǥ ���� ���� ����)
        Vector3 aim = (targetPosition - bulletPoint.position).normalized;

        // �ѱ� �÷���
        GameObject flashFX = PoolManager.instance.ActivateObj(1); // Ǯ���� �÷���fx ��������
        SetObjPosition(flashFX, bulletPoint); // bulletPoint ��ġ�� ��ġ
        flashFX.transform.rotation = Quaternion.LookRotation(aim, Vector3.up); // ���� �������� ȸ��

        // ź��
        GameObject caseFX = PoolManager.instance.ActivateObj(2); // Ǯ���� ź�� fx ��������
        SetObjPosition(caseFX, bulletCasePoint); // ź�� ���� ��ġ�� ��ġ


        // �Ѿ�
        GameObject bullet = PoolManager.instance.ActivateObj(0); // Ǯ�� �Ѿ� ������Ʈ ��������
        SetObjPosition(bullet, bulletPoint); // �ѱ� ��ġ�� ����
        bullet.transform.rotation = Quaternion.LookRotation(aim, Vector3.up); // ���� �������� ȸ��

        // ���� Ÿ�� ����
        Ray ray = new Ray(bulletPoint.position, aim); // �ѱ� ���� ���� �������� Ray ����
        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) // 100m �Ÿ����� �浹 �˻�
        {
            ZombieController zombie = hit.collider.GetComponent<ZombieController>();
            BossController boss = hit.collider.GetComponent<BossController>();

            // ���� �¾��� ���
            if (zombie != null)
            {
                zombie.TakeDamage(1); // ���� ������ ó��
                SpawnBloodEffect(hit.point, Quaternion.LookRotation(hit.normal)); // �� ȿ�� ����
            }
            else if (boss != null) // ���񺸽��� �¾��� ���
            {
                boss.TakeDamage(1); // ���� ������ ó��
                SpawnBloodEffect(hit.point, Quaternion.LookRotation(hit.normal)); // �� ȿ�� ����
            }
        }
    }

    public void SpawnMissile(Vector3 spawnPos, Quaternion rotation)
    {
        GameObject missile = PoolManager.instance.ActivateObj(5);
        missile.transform.position = spawnPos;
        missile.transform.rotation = rotation;

        Rigidbody rb = missile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = missile.transform.forward * 20f;
        }
    }

    public void SpawnBloodEffect(Vector3 position, Quaternion rotation)
    {
        GameObject blood = PoolManager.instance.ActivateObj(6);
        blood.transform.position = position;
        blood.transform.rotation = rotation;
        StartCoroutine(DisableBloodEffect(blood, 0.7f));

    }

    private IEnumerator DisableBloodEffect(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    public void SpawnExplosionEffect(Vector3 _position, Quaternion _rotation)
    {
        GameObject explosion = PoolManager.instance.ActivateObj(7);
        explosion.transform.position = _position;
        explosion.transform.rotation = _rotation;

        if (explosionSFX != null && audioResource != null)
            audioResource.PlayOneShot(explosionSFX);

        StartCoroutine(DisableExplosionEffect(explosion, 0.7f));
    }

    private IEnumerator DisableExplosionEffect(GameObject _obj, float _delay)
    {
        yield return new WaitForSeconds(_delay);
        if (_obj != null)
        {
            _obj.SetActive(false);
        }
    }

    // ������ ��� ������ ��ġ�� �� ���� �����ϴ� �Լ�
    private void SpawnAllHealPacks()
    {
        foreach (Transform point in healPackPoints)// �̸� ������ ���� ���� ��ġ���� ��ȸ
        {
            // ������Ʈ Ǯ���� ���� ��ü�� ������
            GameObject pack = PoolManager.instance.ActivateObj(healPackPoolIndex);

            // �ش� ��ġ���� �ణ ���ʿ� ���� ��ġ(���� ���� �ߵ���)
            pack.transform.position = point.position + Vector3.up * 0.5f;

            // ȸ�� �ʱ�ȭ(�⺻ �������� ����)
            pack.transform.rotation = Quaternion.identity;

            // Ȱ��ȭ�� ���� ����Ʈ�� �߰��Ͽ� ���� ����� �� �ߺ� üũ�� ���
            activeHealPacks.Add(pack);
        }
    }

    // ������ �ֱ������� üũ�ϰ� ����� ������ �ٽ� �����ϴ� ��ƾ
    private IEnumerator HealPackRespawnRoutine()
    {
        while (true)
        {
            // 2�� ��� �� �ݺ�
            yield return new WaitForSeconds(120f);

            foreach (Transform point in healPackPoints)
            {
                // ���� ��ġ�� ������ �����ϴ��� ���� �˻�
                bool exists = activeHealPacks.Exists(pack =>
                    pack != null && pack.activeInHierarchy && // null �ƴϰ� Ȱ�� �����̸�
                    Vector3.Distance(pack.transform.position, point.position + Vector3.up * 0.5f) < 0.1f); // ��ġ ��ġ ����

                if (!exists) // ������ ���� ����
                {
                    GameObject pack = PoolManager.instance.ActivateObj(healPackPoolIndex);
                    pack.transform.position = point.position + Vector3.up * 0.5f;
                    pack.transform.rotation = Quaternion.identity;
                    activeHealPacks.Add(pack);
                }
            }
        }
    }

    private void SetObjPosition(GameObject obj, Transform targetTransform)
    {
        obj.transform.position = targetTransform.position;
    }

    private IEnumerator ZombieSpawn()
    {
        for (int i = 0; i < maxZombies; i++)
        {
            GameObject enemy = PoolManager.instance.ActivateObj(4);
            SetObjPosition(enemy, spawnPoints[Random.Range(0, spawnPoints.Length)].transform);
            zombieSpawnedCount++;
            yield return new WaitForSeconds(2f);
        }
    }

    public void OnZombieKilled()
    {
        zombieKillCount++;

        if (zombieKillCount >= maxZombies && !bossSpawned)
        {
            bossSpawned = true;
            SpawnBossZombie();
        }
    }

    private void SpawnBossZombie()
    {
        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        Instantiate(bossZombiePrefab, spawnPos, Quaternion.identity);
        Debug.Log("���� ���� ����!");
    }

    public void ReroadClip()
    {
        GameObject clipFX = PoolManager.instance.ActivateObj(3);
        SetObjPosition(clipFX, weaponClipPoint);
        InitBullet();
    }

    private void InitBullet()
    {
        currentBullet = maxBullet;
    }

    public void GameOver()
    {
        StartCoroutine(ShowGameOver());
    }

    private IEnumerator ShowGameOver()
    {
        yield return new WaitForSeconds(3f);

        if (gameoverSFX != null)
        {
            audioResource.PlayOneShot(gameoverSFX);
            Debug.Log("GameOver ���� �����");
        }

        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 0;

        gameOverUI.SetActive(true);
    }

    public void RestartButton()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }

    public void MainMenuButton()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void GameClear()
    {
        StartCoroutine(ShowGameClear());
    }

    private IEnumerator ShowGameClear()
    {
        yield return new WaitForSeconds(1f);
        if(clearSFX != null && !audioResource.isPlaying)
        audioResource.PlayOneShot(clearSFX);
        Time.timeScale = 0;
        gameClearUI.SetActive(true);
    }

    public bool HasAmmo()
    {
        return currentBullet > 0;
    }
}