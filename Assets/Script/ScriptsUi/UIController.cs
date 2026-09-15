using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject startMenuPanel;

    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private GameObject gamePanel;

    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private GameObject clearPanel;

    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private GameObject keyconfigPanel;

    [Header("References")]
    [SerializeField]
    private CharacterState characterState;

    [SerializeField]
    private Boss boss;

    [Header("Audio")]
    [SerializeField]
    private AudioClip mainMenuSound;

    [SerializeField]
    private AudioClip gameOverSound;

    [SerializeField]
    private AudioClip clearSound;

    [SerializeField]
    private AudioClip inGameSound;

    [SerializeField]
    private AudioClip clickSound;

    [Header("Controllers")]
    [SerializeField]
    private SettingsController settingsController;

    [Header("Clear Panel Text")]
    [SerializeField]
    private TextMeshProUGUI clearDamageDealtText;

    [SerializeField]
    private TextMeshProUGUI clearDamageTakenText;

    [SerializeField]
    private TextMeshProUGUI clearAttackCountText;

    [SerializeField]
    private TextMeshProUGUI clearHitCountText;

    [SerializeField]
    private TextMeshProUGUI clearTimeText;

    [Header("GameOver Panel Text")]
    [SerializeField]
    private TextMeshProUGUI overDamageDealtText;

    [SerializeField]
    private TextMeshProUGUI overDamageTakenText;

    [SerializeField]
    private TextMeshProUGUI overAttackCountText;

    [SerializeField]
    private TextMeshProUGUI overHitCountText;

    [SerializeField]
    private TextMeshProUGUI overBossHpText;

    [SerializeField]
    private float logDisplayDelay = 3f;

    private int damageDeal;
    private int damageTaken;
    private int attackCount;
    private int hitCount;
    private float runTimer;
    private bool timerRunning;
    private static bool restartGame;

    private GameObject prevPanel;

    private bool gameOver = false;

    InputAction cancel;
    InputAction pause;

    private void Awake()
    {
        startMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        clearPanel.SetActive(false);
        pausePanel.SetActive(false);
        keyconfigPanel.SetActive(false);
        Time.timeScale = 0f;

        foreach (var actionMap in InputSystem.actions.actionMaps)
        {
            actionMap.Disable();
        }
        CursorVisible();
    }

    private void Start()
    {
        if (AudioManager.Instance != null && mainMenuSound != null)
        {
            AudioManager.Instance.PlayBGM(mainMenuSound);
        }
        //캐릭터 인스턴스 탐색
        if (characterState == null || !characterState.gameObject.activeInHierarchy)
        {
            characterState = FindAnyObjectByType<CharacterState>();
        }

        if (characterState != null)
        {
            characterState.Damaged += OnPlayerDamaged;
        }

        // boss도 동일하게 비활성 참조면 씬에서 탐색
        if (boss == null || !boss.gameObject.activeInHierarchy)
        {
            boss = FindAnyObjectByType<Boss>();
        }

        if (boss != null)
        {
            boss.OnClear += OnClear;
            boss.OnDamage += OnBossDamaged;
        }
        if (restartGame)
        {
            restartGame = false;
            settingsController.ApplySettingData();
            OnStartGame();
        }

        cancel = InputSystem.actions.FindAction("Cancel");
        cancel.performed += OnPause;

        pause = InputSystem.actions.FindAction("Pause");
        pause.performed += OnPause;
    }

    private void OnDisable()
    {
        cancel.performed -= OnPause;
        pause.performed -= OnPause;
    }

    private void OnDestroy() //
    {
        if (characterState != null)
        {
            characterState.Damaged -= OnPlayerDamaged;
        }

        if (boss != null)
        {
            boss.OnClear -= OnClear;
            boss.OnDamage -= OnBossDamaged;
        }
    }

    private void Update()
    {
        if (timerRunning)
            runTimer += Time.deltaTime;
        CheckDeath();
    }

    private bool logDisplayed;

    private void CheckDeath()
    {
        if (gameOver)
            return;
        if (logDisplayed)
            return;
        if (characterState == null)
            return;
        if (!gamePanel.activeSelf)
            return;
        if (!characterState.IsDead)
            return;

        gameOver = true;
        logDisplayed = true;
        StartCoroutine(ShowGameOverPanel());
    }

    public void OnStartGame()
    {
        AudioManager.Instance.PlaySE(clickSound);
        AudioManager.Instance.PlayBGM(inGameSound);
        gameOver = false;
        ResetStats();
        timerRunning = true;

        startMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (clearPanel != null)
            clearPanel.SetActive(false);
        gamePanel.SetActive(true);
        settingsController.StartCount();

        Time.timeScale = 1f;
        CursorInvisible();
    }

    public void OnOpenSettings()
    {
        AudioManager.Instance.PlaySE(clickSound);

        prevPanel = pausePanel.activeSelf ? pausePanel : startMenuPanel;
        pausePanel.SetActive(false);
        startMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        CursorVisible();
    }

    public void OnCloseSettings()
    {
        AudioManager.Instance.PlaySE(clickSound);
        SaveManager.Instance.Save();
        settingsPanel.SetActive(false);
        if (prevPanel != null)
        {
            prevPanel.SetActive(true);
        }
        CursorVisible();
    }

    public void OnOpenKeyConfig()
    {
        AudioManager.Instance.PlaySE(clickSound);
        keyconfigPanel.SetActive(true);
        startMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        CursorVisible();
    }

    public void OnGameRestart()
    {
        AudioManager.Instance.PlaySE(clickSound);
        restartGame = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMainMenu()
    {
        AudioManager.Instance.PlaySE(clickSound);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (characterState.currentState == CharacterState.StateType.Die)
        {
            return;
        }
        if (!gamePanel.activeSelf && !pausePanel.activeSelf)
            return;

        if (pausePanel.activeSelf)
        {
            OnResume();
        }
        else
        {
            pausePanel.SetActive(true);
            gamePanel.SetActive(false);
            Time.timeScale = 0f;
            CursorVisible();
        }
    }

    public void OnResume()
    {
        AudioManager.Instance.PlaySE(clickSound);
        pausePanel.SetActive(false);
        gamePanel.SetActive(true);
        Time.timeScale = 1f;
        CursorInvisible();
    }

    public void OnGameOver()
    {
        timerRunning = false;
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        settingsController.StopCount();
        UpdateGameOverTexts();
        SaveManager.Instance.Save();
        CursorVisible();
        StartCoroutine(ShowGameOverLogStats());
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    private IEnumerator ShowGameOverPanel()
    {
        timerRunning = false;

        yield return new WaitForSecondsRealtime(logDisplayDelay);

        logDisplayed = false;
        AudioManager.Instance.PlayBGM(gameOverSound);
        OnGameOver();
    }

    public void OnClear()
    {
        if (gameOver)
            return;
        if (logDisplayed)
            return;
        gameOver = true;
        logDisplayed = true;
        StartCoroutine(ShowClearPanel());
    }

    private IEnumerator ShowClearPanel()
    {
        timerRunning = false;
        yield return new WaitForSecondsRealtime(logDisplayDelay);

        logDisplayed = false;

        AudioManager.Instance.PlayBGM(clearSound);
        gamePanel.SetActive(false);
        pausePanel.SetActive(false);
        clearPanel.SetActive(true);
        Time.timeScale = 0f;
        settingsController.StopCount();
        SaveManager.Instance.CurrentData.stageClearCount++;
        UpdateClearTexts();
        SaveManager.Instance.Save();
        CursorVisible();
        StartCoroutine(ShowClearLogStats());
    }

    public void CursorVisible()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        InputSystem.actions.FindActionMap("UI").Enable();
        InputSystem.actions.FindActionMap("Player").Disable();
    }

    public void CursorInvisible()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        InputSystem.actions.FindActionMap("UI").Disable();
        InputSystem.actions.FindActionMap("Player").Enable();
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnPlayerDamaged(int dmg)
    {
        if (gameOver)
            return;
        damageTaken += dmg;
        hitCount++;
    }

    private void OnBossDamaged(int dmg)
    {
        if (gameOver)
            return;
        damageDeal += dmg;
        attackCount++;
    }

    private void ResetStats()
    {
        damageDeal = 0;
        damageTaken = 0;
        attackCount = 0;
        hitCount = 0;
        runTimer = 0f;
        timerRunning = false;
    }

    private string FormatTime(float seconds)
    {
        int min = (int)(seconds / 60);
        int sec = (int)(seconds % 60);
        return $"{min:D2}:{sec:D2}";
    }

    private void UpdateClearTexts()
    {
        clearDamageDealtText.text = $"{damageDeal:N0}";
        clearDamageTakenText.text = $"{damageTaken:N0}";
        clearAttackCountText.text = $"{attackCount}";
        clearHitCountText.text = $"{hitCount}";
        clearTimeText.text = FormatTime(runTimer);
    }

    private void UpdateGameOverTexts()
    {
        overDamageDealtText.text = $"{damageDeal:N0}";
        overDamageTakenText.text = $"{damageTaken:N0}";
        overAttackCountText.text = $"{attackCount}";
        overHitCountText.text = $"{hitCount}";

        float ratio = boss.CurrentHp / (float)boss.data.BossHp * 100f;
        overBossHpText.text = $"{ratio:F0}%";
    }

    //게임오버/클리어 패널의 로그 텍스트 순차적으로 띄우기

    private IEnumerator ShowClearLogStats()
    {
        float delay = 0.3f;
        float duration = 0.2f;

        clearDamageDealtText.gameObject.SetActive(false);
        clearDamageTakenText.gameObject.SetActive(false);
        clearAttackCountText.gameObject.SetActive(false);
        clearHitCountText.gameObject.SetActive(false);
        clearTimeText.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(clearDamageDealtText, damageDeal, duration, "{0:N0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(clearDamageTakenText, damageTaken, duration, "{0:N0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(clearAttackCountText, attackCount, duration, "{0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(clearHitCountText, hitCount, duration, "{0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpTimeText(clearTimeText, runTimer, duration);
    }

    private IEnumerator ShowGameOverLogStats()
    {
        float delay = 0.3f;
        float duration = 0.2f;
        float ratio = boss.CurrentHp / (float)boss.data.BossHp * 100f;

        overDamageDealtText.gameObject.SetActive(false);
        overDamageTakenText.gameObject.SetActive(false);
        overAttackCountText.gameObject.SetActive(false);
        overHitCountText.gameObject.SetActive(false);
        overBossHpText.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(overDamageDealtText, damageDeal, duration, "{0:N0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(overDamageTakenText, damageTaken, duration, "{0:N0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(overAttackCountText, attackCount, duration, "{0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(overHitCountText, hitCount, duration, "{0}");
        yield return new WaitForSecondsRealtime(delay);
        yield return CountUpText(overBossHpText, Mathf.RoundToInt(ratio), duration, "{0}%");
        //중복 요소 줄이기 고려.
    }

    //오버 패널에서 숫자들이 0부터 차례대로 올라오도록 하는 코루틴 메서드 작성
    private IEnumerator CountUpText(
        TextMeshProUGUI text,
        int target,
        float duration,
        string format = "{0}"
    )
    {
        text.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; //timescale이 0이므로 unscaled 사용
            float time = Mathf.Clamp01(elapsed / duration);
            int value = Mathf.RoundToInt(Mathf.Lerp(0, target, time));
            text.text = string.Format(format, value);
            yield return null;
        }

        text.text = string.Format(format, target);
    }

    //시간 표시용
    private IEnumerator CountUpTimeText(TextMeshProUGUI text, float target, float duration)
    {
        text.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; //timescale이 0이므로 unscaled 사용
            float time = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(0, target, time);
            text.text = FormatTime(value);
            yield return null;
        }
        text.text = FormatTime(target);
    }
    //메서드 역할이 유사한게 있으므로 통합 고려.
}
