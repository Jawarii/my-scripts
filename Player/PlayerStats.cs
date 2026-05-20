using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using Cinemachine;

public class PlayerStats : MonoBehaviour
{
    // Base Stats Constants
    private const float BASE_HP_MULTIPLIER = 100f;
    private const float BASE_ATTACK_MULTIPLIER = 20f;
    private const float BASE_DEFENSE_MULTIPLIER = 15f;
    private const float STAT_GROWTH_RATE = 1.1f;

    // Combat Constants
    private const float ARMOR_DIVISOR = 166f;
    private const float HIT_INDICATOR_DURATION = 0.15f;

    // Cached Components
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D playerCollider;
    private Transform playerTransform;
    private Canvas levelUpCanvasComponent;
    private Image levelUpImage;
    private TMP_Text levelUpText;
    private Transform[] respawnPoints;

    // Cached Calculations
    private float levelMultiplier = 1f;  // Will store Mathf.Pow(STAT_GROWTH_RATE, lvl - 1)
    private float cachedArmorEfficiency = 1f;

    // Combat Stats
    [Header("Combat Stats")]
    public float baseHp = 0;
    public float maxHp = 0;
    public float currentHp = 0;
    public float baseAttack = 0;
    public float attack = 0;
    public float atkSpd = 1.0f;
    public float baseDefense = 0;
    private float _defense = 0;
    public float defense 
    { 
        get => _defense;
        set
        {
            _defense = value;
            UpdateArmorEfficiency();
        }
    }
    public float critRate = 5.0f;
    public float critDmg = 150.0f;
    public float staggerDmg = 120f;
    public float speed = 1.0f;
    public float hpRecovery = 1f;
    public float cdReduction = 0f;

    // Bonus Stats
    [Header("Bonus Stats")]
    public float atkMulti = 0f;
    public float speedMulti = 0f;
    public float atkSpeedMulti = 0f;
    public float defenseSpeedMulti = 0f;
    public float critRateMulti = 0f;
    public float hpMulti = 0f;
    public float critDmgMulti = 0f;
    public float hpRecoveryMulti = 0f;

    // Level Stats
    [Header("Level Stats")]
    public float lvl = 1;
    public float currentExp = 0;
    public float maxExp;

    // Skill Points for skill tree system
    public int skillPoints = 0;

    // Variables
    [Header("Variables")]
    public float timeSince = 0;
    public Skills playerSkills;
    public float hpRecCd = 3f;
    public float hpRecCdCur = 0;
    public float hitIndicator = 0f;
    private Color baseColor;
    private float prevHp;

    // PlayTime
    [Header("PlayTime")]
    public float playTime = 0.0f;

    // Sound
    [Header("Sound")]
    public AudioSource source;
    public AudioClip clip;
    public AudioSource levelUpSource;
    public AudioClip levelUpClip;
    public AudioClip deathClip;
    public AudioClip notifyClip;

    // Popup
    [SerializeField] private DamagePopup damagePopup;

    // Spawner & Boss Objects
    public GameObject bossObj;
    public float timeScaleMod = 1.0f;
    public bool isImmune = false;
    public Canvas levelUpCanvas;
    public GameObject respawnLocations;
    public bool isDead = false;
    public PlayerMovement playerMovement;
    public float temSpeed = 0;
    public PlayerInformation playerInformation;
    public CinemachineImpulseSource impulseSource;

    void Start()
    {
        // Cache components
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        playerInformation = transform.GetComponent<PlayerInformation>();
        playerCollider = GetComponent<Collider2D>();
        playerTransform = transform;
        
        // Cache UI components
        levelUpCanvasComponent = levelUpCanvas.GetComponent<Canvas>();
        levelUpImage = levelUpCanvas.GetComponentInChildren<Image>();
        levelUpText = levelUpCanvas.GetComponentInChildren<TMP_Text>();
        
        respawnLocations = GameObject.Find("RespawnLocations");
        source = GameObject.Find("PlayerGettingHitSource").GetComponent<AudioSource>();
        
        // Cache respawn points
        respawnPoints = new Transform[respawnLocations.transform.childCount];
        for (int i = 0; i < respawnLocations.transform.childCount; i++)
        {
            respawnPoints[i] = respawnLocations.transform.GetChild(i);
        }
        
        // Calculate initial stats
        UpdateLevelMultiplier();
        maxExp = Mathf.Pow(lvl, 1.8f) * 100f;

        maxHp -= baseHp;
        baseHp = (int)(BASE_HP_MULTIPLIER * levelMultiplier);
        maxHp += baseHp;
        currentHp = maxHp;
        prevHp = currentHp;

        attack -= baseAttack;
        baseAttack = (int)(BASE_ATTACK_MULTIPLIER * levelMultiplier);
        attack += baseAttack;

        defense -= baseDefense;
        baseDefense = (int)(BASE_DEFENSE_MULTIPLIER * levelMultiplier);
        defense += baseDefense;

        baseColor = spriteRenderer.color;
        UpdateArmorEfficiency();

        levelUpSource = GameObject.Find("LevelUpAudioSource").GetComponent<AudioSource>();
    }

    private void UpdateLevelMultiplier()
    {
        levelMultiplier = Mathf.Pow(STAT_GROWTH_RATE, lvl - 1);
    }

    private void UpdateArmorEfficiency()
    {
        cachedArmorEfficiency = defense / ARMOR_DIVISOR + 1f;
    }

    void Update()
    {
        playTime += Time.deltaTime;
        HandleHitIndicator();
    }

    void FixedUpdate()
    {
        timeSince += Time.deltaTime;
        hpRecCdCur += Time.deltaTime;
        
        if (prevHp != currentHp)
        {
            timeSince = 0;
            prevHp = currentHp;
        }

        if (currentExp >= maxExp)
        {
            LevelUp();
        }
        if (hpRecCdCur >= hpRecCd && currentHp < maxHp && !isDead)
        {
            currentHp += hpRecovery;
            if (currentHp > maxHp)
                currentHp = maxHp;
            hpRecCdCur = 0;
        }
        CheckLevelMilestones();
    }

    private void CheckLevelMilestones()
    {
        if (lvl >= 10) playerInformation.lvl1IsComplete = true;
        if (lvl >= 20) playerInformation.lvl2IsComplete = true;
        if (lvl >= 30) playerInformation.lvl3IsComplete = true;
    }

    private void HandleHitIndicator()
    {
        if (hitIndicator > 0)
        {
            hitIndicator -= Time.deltaTime;
        }
        else
        {
            spriteRenderer.color = baseColor;
        }
    }

    public void IncreaseExp(float exp)
    {
        currentExp += exp;
    }

    public void LevelUp()
    {
        lvl++;
        skillPoints += 1; // Grant 1 skill point per level up
        CheckLevelMilestones();
        currentExp = currentExp - maxExp;
        maxExp = Mathf.Pow(lvl, 1.8f) * 100f;
        IncreaseStats();
        levelUpSource.PlayOneShot(levelUpClip);
        StartCoroutine(HandleLevelUpPanel());
    }

    public void IncreaseStats()
    {
        UpdateLevelMultiplier();
        
        maxHp -= baseHp;
        baseHp = (int)(BASE_HP_MULTIPLIER * levelMultiplier);
        maxHp += baseHp;
        if (!isDead)
            currentHp = maxHp;

        attack -= baseAttack;
        baseAttack = (int)(BASE_ATTACK_MULTIPLIER * levelMultiplier);
        attack += baseAttack;

        defense -= baseDefense;
        baseDefense = (int)(BASE_DEFENSE_MULTIPLIER * levelMultiplier);
        defense += baseDefense;
        
        UpdateArmorEfficiency();
    }

    public void TakeDamage(int damage, bool isCrit)
    {
        if (isImmune) return;
        
        damage = (int)(damage / cachedArmorEfficiency);
        if (damage < 1) damage = 1;
        
        source.Stop();
        currentHp -= damage;
        damagePopup.isPlayer = true;
        damagePopup.Create(transform.position, Mathf.Abs(transform.localScale.y / 2f), damage, isCrit);
        spriteRenderer.color = Color.red;
        hitIndicator = HIT_INDICATOR_DURATION;
        source.PlayOneShot(clip);
        
        if (currentHp <= 0)
        {
            currentHp = 0;
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        isDead = true;
        impulseSource.GenerateImpulse();
        animator.SetBool("isDead", true);
        source.PlayOneShot(deathClip);
        StartCoroutine(DeathRoutine());
    }

    private void SetComponentsState(bool enabled)
    {
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component != this && component != playerMovement)
            {
                component.enabled = enabled;
            }
        }
        
        if (playerCollider != null) playerCollider.enabled = enabled;
        
        if (transform.childCount >= 2)
        {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().enabled = enabled;
            transform.GetChild(1).gameObject.SetActive(enabled);
        }
    }

    private IEnumerator DeathRoutine()
    {
        SetComponentsState(false);
        temSpeed = playerMovement.speed;
        playerMovement.speed = 0;
        playerMovement.canMove = false;
        playerMovement.canDash = false;

        yield return new WaitForSeconds(5f);

        Transform closestRespawn = FindClosestRespawnPoint();
        SetComponentsState(true);

        currentHp = maxHp;
        if (closestRespawn != null)
        {
            transform.position = closestRespawn.position;
        }
        animator.SetBool("isDead", false);
        isDead = false;
        playerMovement.speed = temSpeed;
        playerMovement.canMove = true;
        playerMovement.canDash = true;
    }

    private Transform FindClosestRespawnPoint()
    {
        Transform closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = playerTransform.position;

        foreach (Transform respawnPoint in respawnPoints)
        {
            float distance = Vector3.Distance(currentPosition, respawnPoint.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = respawnPoint;
            }
        }

        return closest;
    }

    private void ShowLevelUpPanel(string message, float duration)
    {
        levelUpCanvasComponent.enabled = true;
        levelUpText.text = message;
        StartCoroutine(HideLevelUpPanelAfterDelay(duration));
    }

    private IEnumerator HideLevelUpPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        levelUpCanvasComponent.enabled = false;
    }

    public IEnumerator HandleLevelUpPanel()
    {
        ShowLevelUpPanel("You Leveled Up!", 3f);
        yield return new WaitForSeconds(4f);

        if (lvl == 3 || lvl == 6 || lvl == 9)
        {
            ShowLevelUpPanel("You Unlocked New Skills, Press [K]", 3f);
            source.PlayOneShot(notifyClip);
        }
        else if (lvl == 10 || lvl == 20 || lvl == 30)
        {
            ShowLevelUpPanel("You Unlocked The Boss of This Area", 3f);
            source.PlayOneShot(notifyClip);
            yield return new WaitForSeconds(4f);
            ShowLevelUpPanel("You Unlocked The Arena of This Area", 3f);
            source.PlayOneShot(notifyClip);
        }
    }
}
