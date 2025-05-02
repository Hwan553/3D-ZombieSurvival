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

    // 사격 함수
    public void Shooting(Vector3 targetPosition, ZombieController zombieCtr)
    {
        // 탄약이 없으면 사격 불가
        if (currentBullet <= 0) return;

        // 탄약 1발 소모
        currentBullet--;

        // 조준 방향 계산(총구 -> 목표 지점 방향 벡터)
        Vector3 aim = (targetPosition - bulletPoint.position).normalized;

        // 총구 플래시
        GameObject flashFX = PoolManager.instance.ActivateObj(1); // 풀에서 플래시fx 꺼내오기
        SetObjPosition(flashFX, bulletPoint); // bulletPoint 위치에 배치
        flashFX.transform.rotation = Quaternion.LookRotation(aim, Vector3.up); // 조준 방향으로 회전

        // 탄피
        GameObject caseFX = PoolManager.instance.ActivateObj(2); // 풀에서 탄피 fx 꺼내오기
        SetObjPosition(caseFX, bulletCasePoint); // 탄피 배출 위치로 배치


        // 총알
        GameObject bullet = PoolManager.instance.ActivateObj(0); // 풀에 총알 오브젝트 꺼내오기
        SetObjPosition(bullet, bulletPoint); // 총구 위치에 생성
        bullet.transform.rotation = Quaternion.LookRotation(aim, Vector3.up); // 조준 방향으로 회전

        // 실제 타격 판정
        Ray ray = new Ray(bulletPoint.position, aim); // 총구 기준 조준 방향으로 Ray 생성
        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) // 100m 거리까지 충돌 검사
        {
            ZombieController zombie = hit.collider.GetComponent<ZombieController>();
            BossController boss = hit.collider.GetComponent<BossController>();

            // 좀비에 맞았을 경우
            if (zombie != null)
            {
                zombie.TakeDamage(1); // 좀비 데미지 처리
                SpawnBloodEffect(hit.point, Quaternion.LookRotation(hit.normal)); // 피 효과 생성
            }
            else if (boss != null) // 좀비보스에 맞았을 경우
            {
                boss.TakeDamage(1); // 보스 데미지 처리
                SpawnBloodEffect(hit.point, Quaternion.LookRotation(hit.normal)); // 피 효과 생성
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

    // 힐팩을 모든 지정된 위치에 한 번씩 생성하는 함수
    private void SpawnAllHealPacks()
    {
        foreach (Transform point in healPackPoints)// 미리 설정된 힐팩 생성 위치들을 순회
        {
            // 오브젝트 풀에서 힐팩 객체를 가져옴
            GameObject pack = PoolManager.instance.ActivateObj(healPackPoolIndex);

            // 해당 위치에서 약간 위쪽에 힐팩 배치(지면 위에 뜨도록)
            pack.transform.position = point.position + Vector3.up * 0.5f;

            // 회전 초기화(기본 방향으로 설정)
            pack.transform.rotation = Quaternion.identity;

            // 활성화된 힐팩 리스트에 추가하여 추후 재생성 시 중복 체크에 사용
            activeHealPacks.Add(pack);
        }
    }

    // 힐팩을 주기적으로 체크하고 사라진 힐팩을 다시 생성하는 루틴
    private IEnumerator HealPackRespawnRoutine()
    {
        while (true)
        {
            // 2분 대기 후 반복
            yield return new WaitForSeconds(120f);

            foreach (Transform point in healPackPoints)
            {
                // 현재 위치에 힐팩이 존재하는지 여부 검사
                bool exists = activeHealPacks.Exists(pack =>
                    pack != null && pack.activeInHierarchy && // null 아니고 활성 상태이며
                    Vector3.Distance(pack.transform.position, point.position + Vector3.up * 0.5f) < 0.1f); // 위치 일치 여부

                if (!exists) // 없으면 새로 생성
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
        Debug.Log("보스 좀비 등장!");
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
            Debug.Log("GameOver 사운드 재생됨");
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