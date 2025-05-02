using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;
    private Camera _camera;
    [SerializeField] private Transform _camPivot;
    private Transform target;

    [SerializeField] private LayerMask targetLayer;

    public float walkSpeed = 4.0f;
    public float runSpeed = 8.0f;
    public float rotationSpeed = 10f;
    public float aimSpeed = 3f;

    private float mouseX;
    private float mouseY;
    private bool isAiming = false;

    private Vector3 defaultCamOffset = new Vector3(0.8f, 1.5f, -3f);
    private Vector3 aimCamOffset = new Vector3(0.4f, 1.7f, -1.5f);

    private ZombieController zombieCtr;
    private BossController bossCtr;

    private float playerMaxHP = 100f;
    public float playerCurrentHP = 0f;

    [SerializeField] private Image playerHPBar;
    [SerializeField] private Transform healEffect;

    private bool isDead = false;

    private float gravity = -9.81f;
    private float verticalVelocity = -0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private AudioClip reloadSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip healSFX;
    private AudioSource audioSource;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        
        audioSource = GetComponent<AudioSource>();

        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (animator == null)
            Debug.LogError("Animator 필요");
        if (controller == null)
            Debug.LogError("CharacterController 필요");

        CameraSetting();

        playerCurrentHP = playerMaxHP;

        GameObject hpBarObject = GameObject.FindGameObjectWithTag("HPBar");
        if (hpBarObject != null)
            playerHPBar = hpBarObject.GetComponent<Image>();

        DisPlayHP();
        controller.enabled = true;
    }

    private void Update()
    {
        HandleCamera();
        HandleMovement();

        if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonDown(0))
            CheckAiming();

        if (Input.GetKeyDown(KeyCode.R))
            CheckReload();
    }

    private void FixedUpdate()
    {
        if (controller == null || !controller.enabled) return;

        if (controller.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity += gravity * Time.fixedDeltaTime;

        Vector3 gravityMove = new Vector3(0, verticalVelocity, 0);
        controller.Move(gravityMove * Time.fixedDeltaTime);
    }

    private void CameraSetting()
    {
        _camera = Camera.main;
        _camPivot = new GameObject("CameraPivot").transform;
        _camPivot.position = transform.position + Vector3.up * 1.8f;
        _camPivot.parent = transform;

        _camera.transform.parent = _camPivot;
        _camera.transform.localPosition = defaultCamOffset;
        _camera.transform.LookAt(_camPivot);
    }

    private void HandleCamera()
    {
        mouseX += Input.GetAxis("Mouse X") * rotationSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * rotationSpeed;
        mouseY = Mathf.Clamp(mouseY, -45, 45);

        _camPivot.rotation = Quaternion.Euler(mouseY, mouseX, 0);

        Vector3 targetPosition = isAiming ? aimCamOffset : defaultCamOffset;
        _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, targetPosition, Time.deltaTime * aimSpeed);
    }

    // 플레이어 이동 함수
    private void HandleMovement()
    {
        // 키보드 입력값을 받아옴
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // 현재 이동 속도 설정
        float moveSpeed = isRunning ? runSpeed : walkSpeed;

        // 입력값이 너무 작을 경우 무시하도록 설정(노이즈 제거용)
        float minMoveThreshold = 0.1f;

        // 애니메이션 블렌딩을 위한 X/Y 값 계산
        // - 일정 입력값 이하일 경우 0으로 처리
        // - 달리기일 경우 1배, 걷기일 경우 0.5배로 감속 처리
        float blendX = Mathf.Abs(horizontal) < minMoveThreshold ? 0 : horizontal * (isRunning ? 1f : 0.5f);
        float blendY = Mathf.Abs(vertical) < minMoveThreshold ? 0 : vertical * (isRunning ? 1f : 0.5f);

        // 애니메이터 파라미터에 블렌딩 값 적용
        animator.SetFloat("Pos X", blendX);
        animator.SetFloat("Pos Y", blendY);

        // 실제 이동 방향 벡터 생성
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // 카메라 방향 기준으로 이동 방향을 변환
        moveDirection = _camPivot.TransformDirection(moveDirection);
        moveDirection.y = 0;// 카메라 방향 기준으로 이동 방향을 변환

        // 일정 이상 이동 입력이 있는 경우
        if (moveDirection.magnitude > 0.1f)
        {
            controller.Move(moveDirection * moveSpeed * Time.deltaTime); // 캐릭터를 moveDirection 방향으로 이동

            // 캐릭터의 회전 방향 설정(이동 방향을 바라보도록)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // 조준 및 사격 처리 함수
    private void CheckAiming()
    {
        // 마우스 우클릭을 누르고 있는 중인지 체크
        if (Input.GetMouseButton(1))
        {
            isAiming = true; // 조준 상태 활성화 플래그 설정

            // 애니메이터의 조준 레이어를 활성화(1번 레이어의 가중치를 1로 설정)
            animator.SetLayerWeight(1, 1);

            Vector3 targetPosition = Vector3.zero; // 조준 대상 위치 초기화
            Transform camTransform = Camera.main.transform; // 카메라의 Transform을 가져옴

            // 카메라 기준으로 정면 방향으로 Ray를 쏘아 충돌 검사
            RaycastHit hit; 
            if (Physics.Raycast(camTransform.position, camTransform.forward, out hit, 100f, targetLayer))
            {
                targetPosition = hit.point; // 충돌한 지점 좌표를 조준 타겟으로 설정

                // 충돌한 오브젝트가 있으면 좀비 or 보스인지 체크
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject != null)
                    zombieCtr = hitObject.GetComponent<ZombieController>();
                bossCtr = hitObject.GetComponent<BossController>();
            }
            else
            {
                // 아무것도 맞추지 못했을 경우 카메라 정면 50m 지점을 타겟으로 설정
                targetPosition = camTransform.position + camTransform.forward * 50f;
            }

            // 조준 타겟에서 y축 제거 수평 회전만 고려
            Vector3 targetAim = targetPosition; // 타켓의 위치
            targetAim.y = transform.position.y; // 높이는 현재 플레이어의 높이로 동일하게 설정
            Vector3 aimDir = (targetAim - transform.position).normalized; // 캐릭터가 조준 방향을 바라보도록 방향 벡터 계산
            transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * 30f); // 부드럽게 조준 방향으로 캐릭터 회전

            if (Input.GetMouseButtonDown(0))
            {
                if (GameManager.instance != null && GameManager.instance.HasAmmo())
                {
                    animator.SetBool(Define.isShoot, true);

                    if (shootSFX != null && audioSource != null)
                        audioSource.PlayOneShot(shootSFX);

                    if (zombieCtr == null || zombieCtr.gameObject == null)
                        Debug.LogWarning("좀비가 이미 삭제되었습니다.");

                    GameManager.instance?.Shooting(targetPosition, zombieCtr);
                }
                else
                {
                    
                    animator.SetBool(Define.isShoot, false);
                }
            }
            else
            {
                animator.SetBool(Define.isShoot, false);
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
            animator.SetLayerWeight(1, 0);
        }
    }

    private void CheckReload()
    {
        isAiming = false;
        animator.SetTrigger(Define.isReload);
        animator.SetBool(Define.isShoot, false);
        Invoke(nameof(PlayReload), 0.5f);
    }

    private void PlayReload()
    {
        if (reloadSFX != null && audioSource != null)
            audioSource.PlayOneShot(reloadSFX);
    }

    public void OnReloadFinished()
    {
        animator.ResetTrigger(Define.isReload);
        animator.SetBool(Define.isShoot, false);
        animator.Play("Rifle Aiming Idle", 0);
    }

    public void ReroadWeaponClip()
    {
        GameManager.instance?.ReroadClip();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        playerCurrentHP += amount;
        playerCurrentHP = Mathf.Min(playerCurrentHP, playerMaxHP);
        DisPlayHP();
    }

    public void PlayHealEffect()
    {
        GameObject healFX = PoolManager.instance.ActivateObj(9);
        healFX.transform.position = healEffect != null ? healEffect.position : transform.position + Vector3.up * 1.5f;
        healFX.transform.rotation = Quaternion.identity;

        if (healSFX != null && !audioSource.isPlaying)
            audioSource.PlayOneShot(healSFX);

        ParticleSystem ps = healFX.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            StartCoroutine(DisableEffectAfterTime(healFX, ps.main.duration));
        }
    }

    private IEnumerator DisableEffectAfterTime(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.CompareTag("Hand"))
        {
            playerCurrentHP -= 5f;
            DisPlayHP();

            if (playerCurrentHP <= 0)
                Die();
        }
    }

    void DisPlayHP()
    {
        if (playerHPBar != null)
            playerHPBar.fillAmount = playerCurrentHP / playerMaxHP;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        controller.enabled = false;

        Invoke(nameof(PlayDeathSFX), 1.15f);

        animator.SetTrigger(Define.isDead);

        
        StartCoroutine(HandleDeath());
    }

    private void PlayDeathSFX()
    {
        if (deathSFX != null && audioSource != null)
            audioSource.PlayOneShot(deathSFX);
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(1f);
        GameManager.instance?.GameOver();
    }

    public void ResetPlayer()
    {
        this.enabled = true;

        playerCurrentHP = playerMaxHP;

        GetComponent<Animator>().Play("Idle");
    }
}
