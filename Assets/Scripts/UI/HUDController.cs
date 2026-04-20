using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the in-game HUD displaying health, ammo, timer, score, and kill feed.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Health Display")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthFill;
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Ammo Display")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponIcon;

    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI redScoreText;
    [SerializeField] private TextMeshProUGUI blueScoreText;

    [Header("Kill Feed")]
    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private GameObject killFeedEntryPrefab;
    [SerializeField] private int maxKillFeedEntries = 5;
    [SerializeField] private float killFeedEntryDuration = 5f;

    [Header("Crosshair")]
    [SerializeField] private Image crosshair;

    [Header("Countdown & Game Over")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Death Screen")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI deathMessageText;
    [SerializeField] private TextMeshProUGUI respawnCountdownText;

    [Header("Damage Indicator")]
    [SerializeField] private Image damageVignette;
    [SerializeField] private float damageFlashDuration = 0.3f;

    [Header("Hit Indicator")]
    [SerializeField] private Image hitIndicatorImage;
    [SerializeField] private float hitIndicatorDuration = 0.25f;
    [SerializeField] private float hitIndicatorSize = 36f;
    [SerializeField] private Color hitIndicatorCenterColor = new Color(0.12f, 0.06f, 0.06f, 1f);
    [SerializeField] private Color hitIndicatorTriangleColor = new Color(0.45f, 0.08f, 0.1f, 1f);

    [Header("Kill Confirmation")]
    [SerializeField] private TextMeshProUGUI killConfirmationText;
    [SerializeField] private float killConfirmationDuration = 2.5f;

    // References
    private PlayerHealth localPlayerHealth;
    private WeaponManager localWeaponManager;
    private GameManager gameManager;
    private WeaponBase currentWeaponForHud;
    private string lastAmmoDisplay = "0 / 0";

    // Kill feed tracking
    private Queue<GameObject> killFeedEntries = new Queue<GameObject>();

    private void Start()
    {
        // Find game manager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnTimerUpdated += UpdateTimer;
            gameManager.OnScoreUpdated += UpdateScore;
        }

        // Hide panels initially
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
        if (damageVignette != null)
        {
            damageVignette.color = new Color(1, 0, 0, 0);
        }

        EnsureHitIndicator();
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null && gameManager.CurrentState == GameState.Playing)
            UpdateTimer(gameManager.MatchTimer);
    }

    private void EnsureHitIndicator()
    {
        if (hitIndicatorImage != null) return;
        Transform parent = (crosshair != null && crosshair.transform.parent != null) ? crosshair.transform.parent : transform;
        var go = new GameObject("HitIndicator");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = crosshair != null ? (crosshair.transform as RectTransform).anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchorMax = crosshair != null ? (crosshair.transform as RectTransform).anchorMax : new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(hitIndicatorSize, hitIndicatorSize);
        rect.anchoredPosition = crosshair != null ? (crosshair.transform as RectTransform).anchoredPosition : Vector2.zero;
        rect.localEulerAngles = new Vector3(0f, 0f, 45f);
        int crosshairSibling = crosshair != null ? crosshair.transform.GetSiblingIndex() : 0;
        go.transform.SetSiblingIndex(crosshairSibling);
        hitIndicatorImage = go.AddComponent<Image>();
        hitIndicatorImage.color = Color.white;
        hitIndicatorImage.raycastTarget = false;
        hitIndicatorImage.sprite = CreateHitIndicatorSprite();
        hitIndicatorImage.preserveAspect = true;
        hitIndicatorImage.enabled = false;
    }

    private Sprite CreateHitIndicatorSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);
        int c = size / 2;
        int boxOut = 6, boxIn = 2;
        Color centerColor = new Color(0.14f, 0.06f, 0.06f, 1f);
        Color triColor = new Color(0.5f, 0.1f, 0.12f, 1f);
        for (int dy = -boxOut; dy <= boxOut; dy++)
            for (int dx = -boxOut; dx <= boxOut; dx++)
            {
                if (Mathf.Abs(dx) > boxIn && Mathf.Abs(dy) > boxIn) continue;
                int px = c + dx, py = c + dy;
                if (px >= 0 && px < size && py >= 0 && py < size)
                    tex.SetPixel(px, py, centerColor);
            }
        var pink = triColor;
        int triLen = 18, triWid = 8;
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                int dx = i - c, dy = j - c;
                if (dy > 0 && dy <= triLen && Mathf.Abs(dx) <= triWid - (dy * triWid / triLen))
                    tex.SetPixel(i, j, pink);
                if (dy < 0 && -dy <= triLen && Mathf.Abs(dx) <= triWid - (-dy * triWid / triLen))
                    tex.SetPixel(i, j, pink);
                if (dx > 0 && dx <= triLen && Mathf.Abs(dy) <= triWid - (dx * triWid / triLen))
                    tex.SetPixel(i, j, pink);
                if (dx < 0 && -dx <= triLen && Mathf.Abs(dy) <= triWid - (-dx * triWid / triLen))
                    tex.SetPixel(i, j, pink);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Coroutine hitIndicatorRoutine;

    /// <summary>
    /// Call when the local player hits an enemy. Shows the hit indicator briefly.
    /// </summary>
    public void ShowHitIndicator()
    {
        if (hitIndicatorImage == null)
        {
            EnsureHitIndicator();
            if (hitIndicatorImage == null) return;
        }
        if (hitIndicatorRoutine != null)
            StopCoroutine(hitIndicatorRoutine);
        hitIndicatorImage.enabled = true;
        hitIndicatorImage.color = new Color(0.85f, 0.85f, 0.9f, 1f);
        if (crosshair != null)
        {
            int crosshairSibling = crosshair.transform.GetSiblingIndex();
            hitIndicatorImage.transform.SetSiblingIndex(crosshairSibling);
        }
        hitIndicatorRoutine = StartCoroutine(HideHitIndicatorAfterDelay());
    }

    private IEnumerator HideHitIndicatorAfterDelay()
    {
        yield return new WaitForSecondsRealtime(hitIndicatorDuration);
        if (hitIndicatorImage != null)
            hitIndicatorImage.enabled = false;
        hitIndicatorRoutine = null;
    }

    /// <summary>
    /// Shows "Eliminated [victimName]" between crosshair and bottom of screen for a short time.
    /// </summary>
    public void ShowKillConfirmation(string victimName)
    {
        EnsureKillConfirmationText();
        if (killConfirmationText == null) return;
        killConfirmationText.text = "Eliminated " + (string.IsNullOrEmpty(victimName) ? "Unknown" : victimName);
        killConfirmationText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideKillConfirmation));
        Invoke(nameof(HideKillConfirmation), killConfirmationDuration);
    }

    private void HideKillConfirmation()
    {
        if (killConfirmationText != null)
            killConfirmationText.gameObject.SetActive(false);
    }

    private void EnsureKillConfirmationText()
    {
        if (killConfirmationText != null) return;
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        Transform parent = (rootCanvas != null) ? rootCanvas.transform : transform;
        GameObject go = new GameObject("KillConfirmationText");
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.22f);
        rect.anchorMax = new Vector2(0.5f, 0.22f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 60f);
        rect.anchoredPosition = Vector2.zero;
        killConfirmationText = go.AddComponent<TextMeshProUGUI>();
        killConfirmationText.fontSize = 28;
        killConfirmationText.fontStyle = TMPro.FontStyles.Bold;
        killConfirmationText.color = new Color(0.5f, 0.12f, 0.12f, 1f);
        killConfirmationText.alignment = TMPro.TextAlignmentOptions.Center;
        killConfirmationText.text = "";
        killConfirmationText.raycastTarget = false;
        killConfirmationText.outlineWidth = 0.25f;
        killConfirmationText.outlineColor = new Color32(0, 0, 0, 255);
        go.SetActive(false);
    }

    /// <summary>
    /// Sets up the HUD for the local player.
    /// </summary>
    public void SetupForLocalPlayer(PlayerHealth health, WeaponManager weapons)
    {
        localPlayerHealth = health;
        localWeaponManager = weapons;

        // Subscribe to health events
        if (localPlayerHealth != null)
        {
            localPlayerHealth.OnHealthChanged += UpdateHealth;
            localPlayerHealth.OnDeath += OnPlayerDeath;
            localPlayerHealth.OnRespawn += OnPlayerRespawn;

            // Initialize health display
            UpdateHealth(localPlayerHealth.CurrentHealth, localPlayerHealth.MaxHealth);
        }

        // Subscribe to weapon events
        if (localWeaponManager != null)
        {
            localWeaponManager.OnWeaponSwitched += OnWeaponSwitched;

            // Initialize weapon display
            if (localWeaponManager.CurrentWeapon != null)
            {
                OnWeaponSwitched(localWeaponManager.CurrentWeapon);
            }
        }

        // Show crosshair when in game
        if (crosshair != null)
        {
            crosshair.enabled = true;
        }
    }

    /// <summary>
    /// Updates the health display.
    /// </summary>
    public void UpdateHealth(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = (float)current / max;
        }

        if (healthText != null)
        {
            healthText.text = current.ToString();
        }

        // Update health bar color based on health percentage
        if (healthFill != null)
        {
            float healthPercent = (float)current / max;

            if (healthPercent > 0.6f)
            {
                healthFill.color = healthyColor;
            }
            else if (healthPercent > 0.3f)
            {
                healthFill.color = damagedColor;
            }
            else
            {
                healthFill.color = criticalColor;
            }
        }
    }

    /// <summary>
    /// Called when the local player's weapon changes.
    /// </summary>
    private void OnWeaponSwitched(WeaponBase weapon)
    {
        if (weapon == null) return;

        // Unsubscribe from old weapon
        if (currentWeaponForHud != null)
        {
            currentWeaponForHud.OnAmmoChanged -= UpdateAmmo;
            currentWeaponForHud.OnMeleeHit -= ShowMeleeHitFeedback;
        }

        currentWeaponForHud = weapon;
        weapon.OnAmmoChanged += UpdateAmmo;
        weapon.OnMeleeHit += ShowMeleeHitFeedback;

        // Update displays
        if (weaponNameText != null)
        {
            weaponNameText.text = weapon.WeaponName;
        }

        if (weaponIcon != null && weapon.Icon != null)
        {
            weaponIcon.sprite = weapon.Icon;
            weaponIcon.enabled = true;
        }
        else if (weaponIcon != null)
        {
            weaponIcon.enabled = false;
        }

        // Update ammo (or "—" for melee)
        UpdateAmmo(weapon.CurrentAmmo, weapon.ReserveAmmo);
    }

    /// <summary>
    /// Updates the ammo display.
    /// </summary>
    public void UpdateAmmo(int current, int reserve)
    {
        if (ammoText == null) return;
        if (currentWeaponForHud != null && !currentWeaponForHud.ShowAmmo)
        {
            ammoText.text = "—";
            lastAmmoDisplay = "—";
            return;
        }
        lastAmmoDisplay = $"{current} / {reserve}";
        ammoText.text = lastAmmoDisplay;
    }

    /// <summary>
    /// Shows "Hit: [name]" in the ammo area for 0.5s, then restores ammo display.
    /// </summary>
    private void ShowMeleeHitFeedback(string hitName)
    {
        if (ammoText == null) return;
        ammoText.text = "Hit: " + hitName;
        CancelInvoke(nameof(RestoreAmmoDisplay));
        Invoke(nameof(RestoreAmmoDisplay), 0.6f);
    }

    private void RestoreAmmoDisplay()
    {
        if (ammoText != null)
        {
            ammoText.text = lastAmmoDisplay;
        }
    }

    /// <summary>
    /// Updates the timer display.
    /// </summary>
    public void UpdateTimer(float timeRemaining)
    {
        EnsureTimerText();
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes}:{seconds:D2}";

        if (timeRemaining <= 30f)
            timerText.color = timeRemaining % 1f < 0.5f ? Color.red : Color.white;
        else
            timerText.color = Color.white;
    }

    private void EnsureTimerText()
    {
        if (timerText != null) return;
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        Transform parent = rootCanvas != null ? rootCanvas.transform : transform;
        var go = new GameObject("MatchTimerText");
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -12f);
        rect.sizeDelta = new Vector2(200f, 32f);
        timerText = go.AddComponent<TextMeshProUGUI>();
        timerText.text = "0:00";
        timerText.fontSize = 24;
        timerText.color = Color.white;
        timerText.alignment = TMPro.TextAlignmentOptions.Center;
        timerText.raycastTarget = false;
    }

    /// <summary>
    /// Updates the score display.
    /// </summary>
    public void UpdateScore(int redScore, int blueScore)
    {
        if (redScoreText != null)
        {
            redScoreText.text = redScore.ToString();
        }

        if (blueScoreText != null)
        {
            blueScoreText.text = blueScore.ToString();
        }
    }

    /// <summary>
    /// Shows the countdown before match start.
    /// </summary>
    public void ShowCountdown(float seconds)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            if (seconds > 0)
            {
                countdownText.text = Mathf.CeilToInt(seconds).ToString();
            }
            else
            {
                countdownText.text = "GO!";
                Invoke(nameof(HideCountdown), 1f);
            }
        }
    }

    private void HideCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called by Game Over panel Return to Lobby button. Leaves the room (NetworkManager loads lobby in OnLeftRoom).
    /// </summary>
    public void ReturnToLobby()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LeaveRoom();
        }
    }

    /// <summary>
    /// Shows the game over screen.
    /// </summary>
    public void ShowGameOver(Team winner)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            Team localTeam = TeamManager.GetLocalPlayerTeam();

            if (winner == Team.None)
            {
                gameOverText.text = "DRAW!";
                gameOverText.color = Color.white;
            }
            else if (winner == localTeam)
            {
                gameOverText.text = "VICTORY!";
                gameOverText.color = Color.green;
            }
            else
            {
                gameOverText.text = "DEFEAT";
                gameOverText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Adds an entry to the kill feed.
    /// </summary>
    public void AddKillFeedEntry(string killerName, string victimName)
    {
        if (killFeedContainer == null || killFeedEntryPrefab == null) return;

        // Create new entry
        GameObject entry = Instantiate(killFeedEntryPrefab, killFeedContainer);

        // Set text
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"{killerName} eliminated {victimName}";
        }

        // Add to queue and schedule removal
        killFeedEntries.Enqueue(entry);
        Destroy(entry, killFeedEntryDuration);

        // Remove oldest if too many
        while (killFeedEntries.Count > maxKillFeedEntries)
        {
            GameObject oldest = killFeedEntries.Dequeue();
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }
    }

    /// <summary>
    /// Shows damage indicator when player is hit.
    /// </summary>
    public void ShowDamageIndicator()
    {
        if (damageVignette != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashDamageVignette());
        }
    }

    private System.Collections.IEnumerator FlashDamageVignette()
    {
        damageVignette.color = new Color(1, 0, 0, 0.5f);

        float elapsed = 0f;
        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.5f, 0f, elapsed / damageFlashDuration);
            damageVignette.color = new Color(1, 0, 0, alpha);
            yield return null;
        }

        damageVignette.color = new Color(1, 0, 0, 0);
    }

    private void OnPlayerDeath()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
        if (deathMessageText != null)
        {
            deathMessageText.text = "You were eliminated!";
        }
        if (respawnCountdownText != null)
        {
            respawnCountdownText.text = "";
        }
        if (localPlayerHealth != null)
        {
            StopAllCoroutines();
            StartCoroutine(DeathCountdownCoroutine());
        }
    }

    private System.Collections.IEnumerator DeathCountdownCoroutine()
    {
        float delay = localPlayerHealth.RespawnDelay;
        float remaining = delay;
        while (remaining > 0)
        {
            if (respawnCountdownText != null)
            {
                respawnCountdownText.text = $"Respawning in {Mathf.CeilToInt(remaining):0}...";
            }
            remaining -= Time.deltaTime;
            yield return null;
        }
        if (respawnCountdownText != null)
        {
            respawnCountdownText.text = "";
        }
    }

    private void OnPlayerRespawn()
    {
        EnsureDeathPanelHidden();
        if (respawnCountdownText != null)
        {
            respawnCountdownText.text = "";
        }
        StopAllCoroutines();
    }

    /// <summary>
    /// Force-hide the death panel (e.g. when respawn completes). Call from PlayerHealth as backup so we never stay stuck on "You were eliminated!".
    /// </summary>
    public void EnsureDeathPanelHidden()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (gameManager != null)
        {
            gameManager.OnTimerUpdated -= UpdateTimer;
            gameManager.OnScoreUpdated -= UpdateScore;
        }

        if (localPlayerHealth != null)
        {
            localPlayerHealth.OnHealthChanged -= UpdateHealth;
            localPlayerHealth.OnDeath -= OnPlayerDeath;
            localPlayerHealth.OnRespawn -= OnPlayerRespawn;
        }
    }
}
