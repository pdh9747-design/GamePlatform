
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    [Header("Player")]
    public float moveSpeed = 15f;
    public Vector3 respawnPosition;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;

    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private Vector3 lastMoveDirection = Vector3.back;

    [Header("UI")]
    public TextMeshProUGUI lifeText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI speedUpText;

    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject gameClearPanel;

    [Header("Cars")]
    public CarMoveLeft[] leftCars;
    public CarMoveRight[] rightCars;
    public CarMoveUp[] upCars;
    public CarMoveDown[] downCars;

    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;

    [Header("Car Difficulty")]
    public float speedIncreaseInterval = 15f;
    public float speedIncreaseAmount = 0.2f;
    public float maxSpeedMultiplier = 2f;

    [Header("Sound Effects")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public AudioClip hitSound;
    public AudioClip gameOverSound;
    public AudioClip victorySound;

    private CharacterController controller;
    private Renderer[] playerRenderers;

    private int life = 3;
    private float timer = 60f;
    private float speedIncreaseTimer = 0f;

    private bool isStarted = false;
    private bool isInvincible = false;
    private bool isGameOver = false;
    private bool isClear = false;

    private bool mobileUp;
    private bool mobileDown;
    private bool mobileLeft;
    private bool mobileRight;

    private int totalGames;
    private int totalClears;
    private int lastClearLife;

    private bool currentRunCounted = false;
    private bool wasRunningBeforePause = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerRenderers = GetComponentsInChildren<Renderer>();

        if (controller == null)
        {
            Debug.LogError(
                "Player에 CharacterController가 없습니다!",
                this
            );
        }
    }

    IEnumerator Start()
    {
        while (GameSettingsLoader.Instance != null &&
               !GameSettingsLoader.Instance.IsLoaded)
        {
            yield return null;
        }

        if (GameSettingsLoader.Instance != null &&
            GameSettingsLoader.Instance.settings != null)
        {
            GameSettingsData settings =
                GameSettingsLoader.Instance.settings;

            moveSpeed = settings.moveSpeed;
            timer = settings.gameTime;
            speedIncreaseInterval = settings.speedIncreaseInterval;
            speedIncreaseAmount = settings.speedIncreaseAmount;
            maxSpeedMultiplier = settings.maxSpeedMultiplier;

            Debug.Log("JSON 설정이 게임에 적용되었습니다.");
        }
        else
        {
            Debug.LogWarning(
                "GameSettingsManager가 없거나 설정 데이터가 없습니다. Inspector 기본값을 사용합니다."
            );
        }

        respawnPosition = transform.position;

        LoadRecordData();

        CarMoveLeft.speedMultiplier = 1f;
        CarMoveRight.speedMultiplier = 1f;

        Time.timeScale = 0f;

        if (startPanel != null)
            startPanel.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameClearPanel != null)
            gameClearPanel.SetActive(false);

        if (speedUpText != null)
            speedUpText.gameObject.SetActive(false);

        UpdateUI();

        Debug.Log(
            "저장된 게임 기록 - 총 게임: " + totalGames +
            " / 총 클리어: " + totalClears +
            " / 마지막 클리어 남은 목숨: " + lastClearLife
        );
    }

    void Update()
    {
        if (!isStarted || isGameOver || isClear)
            return;

        if (controller == null)
            return;

        float x = 0f;
        float z = 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed)
                x -= 1f;

            if (keyboard.dKey.isPressed)
                x += 1f;

            if (keyboard.wKey.isPressed)
                z += 1f;

            if (keyboard.sKey.isPressed)
                z -= 1f;
        }

        if (mobileLeft)
            x -= 1f;

        if (mobileRight)
            x += 1f;

        if (mobileUp)
            z += 1f;

        if (mobileDown)
            z -= 1f;

        Vector3 move = new Vector3(-x, 0f, -z);
        move = Vector3.ClampMagnitude(move, 1f);

        // 마지막 이동 방향 저장
        if (move.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = move.normalized;
        }

        // 대쉬 재사용 대기시간
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Space 키로 대쉬 시작
        if (keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame &&
            !isDashing &&
            dashCooldownTimer <= 0f)
        {
            StartDash();
        }

        // 대쉬 중 이동
        if (isDashing)
        {
            controller.Move(
                dashDirection * dashSpeed * Time.deltaTime
            );

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }
        else
        {
            // 일반 이동
            controller.Move(
                move * moveSpeed * Time.deltaTime
            );
        }

        // 자동차 속도 증가
        speedIncreaseTimer += Time.deltaTime;

        if (speedIncreaseInterval > 0f &&
            speedIncreaseTimer >= speedIncreaseInterval)
        {
            speedIncreaseTimer = 0f;

            bool canIncreaseSpeed =
                CarMoveLeft.speedMultiplier < maxSpeedMultiplier ||
                CarMoveRight.speedMultiplier < maxSpeedMultiplier;

            CarMoveLeft.speedMultiplier = Mathf.Min(
                CarMoveLeft.speedMultiplier + speedIncreaseAmount,
                maxSpeedMultiplier
            );

            CarMoveRight.speedMultiplier = Mathf.Min(
                CarMoveRight.speedMultiplier + speedIncreaseAmount,
                maxSpeedMultiplier
            );

            if (canIncreaseSpeed)
                ShowSpeedUpText();
        }

        // 게임 타이머
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 0f;
            GameClear();
        }

        UpdateUI();
    }

    // 대쉬 시작
    void StartDash()
    {
        isDashing = true;

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        dashDirection = lastMoveDirection.normalized;

        Debug.Log("대쉬!");
    }

    // 모바일 방향 입력
    public void SetMobileDirection(
        MobileMoveButton.MobileDirection direction,
        bool pressed)
    {
        switch (direction)
        {
            case MobileMoveButton.MobileDirection.Up:
                mobileUp = pressed;
                break;

            case MobileMoveButton.MobileDirection.Down:
                mobileDown = pressed;
                break;

            case MobileMoveButton.MobileDirection.Left:
                mobileLeft = pressed;
                break;

            case MobileMoveButton.MobileDirection.Right:
                mobileRight = pressed;
                break;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void LoadRecordData()
    {
        totalGames = PlayerPrefs.GetInt("TotalGames", 0);
        totalClears = PlayerPrefs.GetInt("TotalClears", 0);
        lastClearLife = PlayerPrefs.GetInt("LastClearLife", 0);
    }

    void SaveRecordData()
    {
        PlayerPrefs.SetInt("TotalGames", totalGames);
        PlayerPrefs.SetInt("TotalClears", totalClears);
        PlayerPrefs.SetInt("LastClearLife", lastClearLife);

        PlayerPrefs.Save();

        Debug.Log("게임 기록 저장 완료");
    }

    void RegisterNewGame()
    {
        if (currentRunCounted)
            return;

        totalGames++;
        currentRunCounted = true;

        SaveRecordData();

        Debug.Log("총 게임 횟수: " + totalGames);
    }

    void ShowSpeedUpText()
    {
        if (speedUpText == null)
            return;

        speedUpText.gameObject.SetActive(true);

        CancelInvoke(nameof(HideSpeedUpText));
        Invoke(nameof(HideSpeedUpText), 2f);
    }

    void HideSpeedUpText()
    {
        if (speedUpText != null)
        {
            speedUpText.gameObject.SetActive(false);
        }
    }

    // 차량 충돌 감지
    void OnTriggerEnter(Collider other)
    {
        if (!isStarted || isGameOver || isClear || other == null)
            return;

        // 대쉬 중에도 기존 충돌 판정 유지
        if (other.CompareTag("Car") && !isInvincible)
        {
            PlaySound(hitSound);

            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(
                    hitEffectPrefab,
                    transform.position + Vector3.up,
                    Quaternion.identity
                );

                ParticleSystem ps =
                    effect.GetComponentInChildren<ParticleSystem>();

                if (ps != null)
                {
                    var main = ps.main;

                    Destroy(
                        effect,
                        main.duration +
                        main.startLifetime.constantMax
                    );
                }
                else
                {
                    Destroy(effect, 3f);
                }
            }

            life--;

            UpdateUI();

            Debug.Log("목숨 감소! 남은 목숨: " + life);

            isInvincible = true;

            CancelInvoke(nameof(ResetDamage));
            CancelInvoke(nameof(BlinkPlayer));

            InvokeRepeating(
                nameof(BlinkPlayer),
                0f,
                0.2f
            );

            Invoke(nameof(ResetDamage), 3f);

            if (life <= 0)
                GameOver();
        }
    }

    void BlinkPlayer()
    {
        if (playerRenderers == null)
            return;

        foreach (Renderer playerRenderer in playerRenderers)
        {
            if (playerRenderer != null)
            {
                playerRenderer.enabled =
                    !playerRenderer.enabled;
            }
        }
    }

    void ResetDamage()
    {
        isInvincible = false;

        CancelInvoke(nameof(BlinkPlayer));

        if (playerRenderers == null)
            return;

        foreach (Renderer playerRenderer in playerRenderers)
        {
            if (playerRenderer != null)
            {
                playerRenderer.enabled = true;
            }
        }
    }

    void GameOver()
    {
        isGameOver = true;

        isDashing = false;

        PlaySound(gameOverSound);
        HideSpeedUpText();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        SaveRecordData();

        Time.timeScale = 0f;
    }

    void GameClear()
    {
        if (isClear)
            return;

        isClear = true;

        isDashing = false;

        totalClears++;
        lastClearLife = life;

        SaveRecordData();

        PlaySound(victorySound);

        Debug.Log("게임 클리어!");
        Debug.Log("총 클리어 횟수: " + totalClears);
        Debug.Log("마지막 클리어 남은 목숨: " + lastClearLife);

        HideSpeedUpText();

        if (gameClearPanel != null)
            gameClearPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    void UpdateUI()
    {
        if (lifeText != null)
            lifeText.text = "Life : " + life;

        if (timerText != null)
            timerText.text = "Time : " +
                Mathf.CeilToInt(timer);
    }

    public void StartGame()
    {
        PlaySound(buttonClickSound);

        RegisterNewGame();

        isStarted = true;

        Time.timeScale = 1f;

        if (startPanel != null)
            startPanel.SetActive(false);
    }

    public void RestartGame()
    {
        PlaySound(buttonClickSound);

        Time.timeScale = 1f;

        CancelInvoke(nameof(ResetDamage));
        CancelInvoke(nameof(BlinkPlayer));
        CancelInvoke(nameof(HideSpeedUpText));

        isStarted = true;
        isInvincible = false;
        isGameOver = false;
        isClear = false;

        // 대쉬 초기화
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        lastMoveDirection = Vector3.back;

        life = 3;

        if (GameSettingsLoader.Instance != null &&
            GameSettingsLoader.Instance.settings != null)
        {
            timer =
                GameSettingsLoader.Instance.settings.gameTime;
        }
        else
        {
            timer = 60f;
        }

        speedIncreaseTimer = 0f;

        currentRunCounted = false;
        RegisterNewGame();

        CarMoveLeft.speedMultiplier = 1f;
        CarMoveRight.speedMultiplier = 1f;

        HideSpeedUpText();

        mobileUp = false;
        mobileDown = false;
        mobileLeft = false;
        mobileRight = false;

        // 플레이어 위치 초기화
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = respawnPosition;
            controller.enabled = true;
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (playerRenderers != null)
        {
            foreach (Renderer playerRenderer in playerRenderers)
            {
                if (playerRenderer != null)
                    playerRenderer.enabled = true;
            }
        }

        // 좌우 차량 위치 초기화
        if (leftCars != null)
        {
            foreach (CarMoveLeft car in leftCars)
            {
                if (car != null)
                    car.ResetCarPosition();
            }
        }

        if (rightCars != null)
        {
            foreach (CarMoveRight car in rightCars)
            {
                if (car != null)
                    car.ResetCarPosition();
            }
        }

        // 위아래 차량 위치 초기화
        if (upCars != null)
        {
            foreach (CarMoveUp car in upCars)
            {
                if (car != null)
                    car.ResetCarPosition();
            }
        }

        if (downCars != null)
        {
            foreach (CarMoveDown car in downCars)
            {
                if (car != null)
                    car.ResetCarPosition();
            }
        }

        if (startPanel != null)
            startPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameClearPanel != null)
            gameClearPanel.SetActive(false);

        UpdateUI();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            wasRunningBeforePause =
                isStarted &&
                !isGameOver &&
                !isClear &&
                Time.timeScale > 0f;

            if (wasRunningBeforePause)
                Time.timeScale = 0f;

            SaveRecordData();

            Debug.Log("앱 일시정지 - 게임 기록 저장");
        }
        else
        {
            if (wasRunningBeforePause)
                Time.timeScale = 1f;

            wasRunningBeforePause = false;

            Debug.Log("앱 복귀");
        }
    }

    void OnApplicationQuit()
    {
        SaveRecordData();
    }

    public void QuitGame()
    {
        PlaySound(buttonClickSound);

        SaveRecordData();

        Application.Quit();
    }
}
