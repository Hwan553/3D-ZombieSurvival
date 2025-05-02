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
            Debug.LogError("Animator �ʿ�");
        if (controller == null)
            Debug.LogError("CharacterController �ʿ�");

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

    // �÷��̾� �̵� �Լ�
    private void HandleMovement()
    {
        // Ű���� �Է°��� �޾ƿ�
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // ���� �̵� �ӵ� ����
        float moveSpeed = isRunning ? runSpeed : walkSpeed;

        // �Է°��� �ʹ� ���� ��� �����ϵ��� ����(������ ���ſ�)
        float minMoveThreshold = 0.1f;

        // �ִϸ��̼� ������ ���� X/Y �� ���
        // - ���� �Է°� ������ ��� 0���� ó��
        // - �޸����� ��� 1��, �ȱ��� ��� 0.5��� ���� ó��
        float blendX = Mathf.Abs(horizontal) < minMoveThreshold ? 0 : horizontal * (isRunning ? 1f : 0.5f);
        float blendY = Mathf.Abs(vertical) < minMoveThreshold ? 0 : vertical * (isRunning ? 1f : 0.5f);

        // �ִϸ����� �Ķ���Ϳ� ���� �� ����
        animator.SetFloat("Pos X", blendX);
        animator.SetFloat("Pos Y", blendY);

        // ���� �̵� ���� ���� ����
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // ī�޶� ���� �������� �̵� ������ ��ȯ
        moveDirection = _camPivot.TransformDirection(moveDirection);
        moveDirection.y = 0;// ī�޶� ���� �������� �̵� ������ ��ȯ

        // ���� �̻� �̵� �Է��� �ִ� ���
        if (moveDirection.magnitude > 0.1f)
        {
            controller.Move(moveDirection * moveSpeed * Time.deltaTime); // ĳ���͸� moveDirection �������� �̵�

            // ĳ������ ȸ�� ���� ����(�̵� ������ �ٶ󺸵���)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // ���� �� ��� ó�� �Լ�
    private void CheckAiming()
    {
        // ���콺 ��Ŭ���� ������ �ִ� ������ üũ
        if (Input.GetMouseButton(1))
        {
            isAiming = true; // ���� ���� Ȱ��ȭ �÷��� ����

            // �ִϸ������� ���� ���̾ Ȱ��ȭ(1�� ���̾��� ����ġ�� 1�� ����)
            animator.SetLayerWeight(1, 1);

            Vector3 targetPosition = Vector3.zero; // ���� ��� ��ġ �ʱ�ȭ
            Transform camTransform = Camera.main.transform; // ī�޶��� Transform�� ������

            // ī�޶� �������� ���� �������� Ray�� ��� �浹 �˻�
            RaycastHit hit; 
            if (Physics.Raycast(camTransform.position, camTransform.forward, out hit, 100f, targetLayer))
            {
                targetPosition = hit.point; // �浹�� ���� ��ǥ�� ���� Ÿ������ ����

                // �浹�� ������Ʈ�� ������ ���� or �������� üũ
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject != null)
                    zombieCtr = hitObject.GetComponent<ZombieController>();
                bossCtr = hitObject.GetComponent<BossController>();
            }
            else
            {
                // �ƹ��͵� ������ ������ ��� ī�޶� ���� 50m ������ Ÿ������ ����
                targetPosition = camTransform.position + camTransform.forward * 50f;
            }

            // ���� Ÿ�ٿ��� y�� ���� ���� ȸ���� ���
            Vector3 targetAim = targetPosition; // Ÿ���� ��ġ
            targetAim.y = transform.position.y; // ���̴� ���� �÷��̾��� ���̷� �����ϰ� ����
            Vector3 aimDir = (targetAim - transform.position).normalized; // ĳ���Ͱ� ���� ������ �ٶ󺸵��� ���� ���� ���
            transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * 30f); // �ε巴�� ���� �������� ĳ���� ȸ��

            if (Input.GetMouseButtonDown(0))
            {
                if (GameManager.instance != null && GameManager.instance.HasAmmo())
                {
                    animator.SetBool(Define.isShoot, true);

                    if (shootSFX != null && audioSource != null)
                        audioSource.PlayOneShot(shootSFX);

                    if (zombieCtr == null || zombieCtr.gameObject == null)
                        Debug.LogWarning("���� �̹� �����Ǿ����ϴ�.");

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
