using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDeathBuffSystem : MonoBehaviour
{
    public static PlayerDeathBuffSystem instance;

    [Header("Player Health")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Survival Timer")]
    public float survivalTime = 0f;
    public float buffThreshold = 30f;

    [Header("Buff Settings")]
    public float damageBuffAmount = 5f;
    public float cooldownReduction = 0.1f; // 10% reduction
    public float movementBuffAmount = 1f;
    public float attackSpeedBuffAmount = 0.2f; // 20% faster
    public float highJumpBuffAmount = 2f; // Extra jump force

    [Header("Current Buffs (Stacking)")]
    [SerializeField] private int damageBuffStacks = 0;
    [SerializeField] private int cooldownBuffStacks = 0;
    [SerializeField] private int movementBuffStacks = 0;
    [SerializeField] private int attackSpeedBuffStacks = 0;
    [SerializeField] private int highJumpBuffStacks = 0;

    [Header("References")]
    public PlayerMovement2D playerMovement;
    public hitScript hitScript;
    public SpriteRenderer playerSprite;
    public Rigidbody2D playerRigidbody;

    [Header("Respawn Settings")]
    public Transform respawnPoint; // Drag empty GameObject here for respawn location

    [Header("UI (Optional)")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI buffsText;

    // Events for other scripts to listen to
    public System.Action<BuffType> OnBuffReceived;
    public System.Action OnPlayerDeath;
    public System.Action OnPlayerRespawn;

    // Safety flags to prevent double spawning
    private bool isDead = false;
    private bool isRespawning = false;

    public enum BuffType
    {
        Damage,
        Cooldown,
        Movement,
        AttackSpeed,
        HighJump
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentHealth = maxHealth;

        // Auto-find components if not assigned
        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
            if (playerSprite == null)
            {
                playerSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement2D>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody2D>();
        }

        if (hitScript == null)
        {
            hitScript = FindObjectOfType<hitScript>();
        }
    }

    private void Update()
    {
        // Update survival timer only if not dead
        if (!isDead)
        {
            survivalTime += Time.deltaTime;
        }

        // Update UI
        UpdateUI();
    }

    public void TakeDamage(float damage)
    {
        // Prevent taking damage if already dead or respawning
        if (isDead || isRespawning)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (!isDead)
        {
            currentHealth += healAmount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        // Prevent multiple death calls
        if (isDead || isRespawning)
        {
            return;
        }

        isDead = true;
        Debug.Log($"Player died after surviving {survivalTime:F1} seconds");
        OnPlayerDeath?.Invoke();

        // Hide player sprite immediately
        if (playerSprite != null)
        {
            playerSprite.enabled = false;
        }

        // Stop all movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        // Disable player movement during death
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Check if player survived long enough for buffs
        if (survivalTime >= buffThreshold)
        {
            GrantRandomBuff();
        }
        else
        {
            Debug.Log("Died too quickly - no buff granted");
        }

        // Use Invoke instead of coroutine if GameObject might become inactive
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RespawnPlayer());
        }
        else
        {
            // Alternative: Use Invoke as backup
            Invoke(nameof(RespawnPlayerDirect), 2f);
        }
    }

    private void GrantRandomBuff()
    {
        // Randomly select a buff type
        BuffType randomBuff = (BuffType)Random.Range(0, System.Enum.GetValues(typeof(BuffType)).Length);

        switch (randomBuff)
        {
            case BuffType.Damage:
                damageBuffStacks++;
                ApplyDamageBuff();
                Debug.Log($"Damage buff granted! Stack: {damageBuffStacks}");
                break;

            case BuffType.Cooldown:
                cooldownBuffStacks++;
                ApplyCooldownBuff();
                Debug.Log($"Cooldown reduction buff granted! Stack: {cooldownBuffStacks}");
                break;

            case BuffType.Movement:
                movementBuffStacks++;
                ApplyMovementBuff();
                Debug.Log($"Movement speed buff granted! Stack: {movementBuffStacks}");
                break;

            case BuffType.AttackSpeed:
                attackSpeedBuffStacks++;
                ApplyAttackSpeedBuff();
                Debug.Log($"Attack speed buff granted! Stack: {attackSpeedBuffStacks}");
                break;

            case BuffType.HighJump:
                highJumpBuffStacks++;
                ApplyHighJumpBuff();
                Debug.Log($"High jump buff granted! Stack: {highJumpBuffStacks}");
                break;
        }

        OnBuffReceived?.Invoke(randomBuff);
    }

    private void ApplyDamageBuff()
    {
        if (hitScript != null)
        {
            hitScript.playerDamage = 10f + (damageBuffStacks * damageBuffAmount);
        }
    }

    private void ApplyCooldownBuff()
    {
        if (playerMovement != null)
        {
            // Reduce attack speed (faster attacks)
            float reduction = cooldownBuffStacks * cooldownReduction;
            playerMovement.attackSpeed = Mathf.Max(0.1f, 1.0f - reduction);
        }
    }

    private void ApplyMovementBuff()
    {
        if (playerMovement != null)
        {
            playerMovement.moveSpeed = 5f + (movementBuffStacks * movementBuffAmount);
        }
    }

    private void ApplyAttackSpeedBuff()
    {
        if (playerMovement != null)
        {
            // Reduce attack cooldown time (faster attacks)
            float speedIncrease = attackSpeedBuffStacks * attackSpeedBuffAmount;
            playerMovement.attackSpeed = Mathf.Max(0.1f, 1.0f - speedIncrease);
        }
    }

    private void ApplyHighJumpBuff()
    {
        if (playerMovement != null)
        {
            // Increase jump force and double jump force
            playerMovement.SetJumpForce(10f + (highJumpBuffStacks * highJumpBuffAmount));
            playerMovement.SetDoubleJumpForce(8f + (highJumpBuffStacks * highJumpBuffAmount));
        }
    }

    private IEnumerator RespawnPlayer()
    {
        // Prevent multiple respawn coroutines
        if (isRespawning)
        {
            yield break;
        }

        isRespawning = true;

        // Brief delay before respawn
        yield return new WaitForSeconds(2f);

        // Reset player state
        currentHealth = maxHealth;
        survivalTime = 0f;

        // Move player to respawn point
        Vector3 respawnPosition;
        if (respawnPoint != null)
        {
            respawnPosition = respawnPoint.position;
            Debug.Log($"Respawning at designated position: {respawnPosition}");
        }
        else
        {
            respawnPosition = Vector3.zero;
            Debug.LogWarning("No respawn point assigned! Respawning at (0,0,0)");
        }

        // Force position update
        transform.position = respawnPosition;

        // Also update rigidbody position to make sure
        if (playerRigidbody != null)
        {
            playerRigidbody.position = new Vector2(respawnPosition.x, respawnPosition.y);
        }

        // Reset physics
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        // Show player sprite again
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        // Re-enable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Re-apply all buffs to maintain stacking
        ReapplyAllBuffs();

        // Reset death flags
        isDead = false;
        isRespawning = false;

        OnPlayerRespawn?.Invoke();
        Debug.Log($"Player successfully respawned at {transform.position}!");
    }

    private void ReapplyAllBuffs()
    {
        ApplyDamageBuff();
        ApplyCooldownBuff();
        ApplyMovementBuff();
        ApplyAttackSpeedBuff();
        ApplyHighJumpBuff();
    }

    private void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = $"Survival Time: {survivalTime:F1}s";
        }

        if (buffsText != null)
        {
            buffsText.text = $"Buffs - DMG:{damageBuffStacks} CD:{cooldownBuffStacks} SPD:{movementBuffStacks} ATK:{attackSpeedBuffStacks} JUMP:{highJumpBuffStacks}";
        }
    }

    // Public methods for other scripts to use
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetSurvivalTime() => survivalTime;

    public int GetBuffStacks(BuffType buffType)
    {
        return buffType switch
        {
            BuffType.Damage => damageBuffStacks,
            BuffType.Cooldown => cooldownBuffStacks,
            BuffType.Movement => movementBuffStacks,
            BuffType.AttackSpeed => attackSpeedBuffStacks,
            BuffType.HighJump => highJumpBuffStacks,
            _ => 0
        };
    }

    // Method to manually trigger death (for testing or other mechanics)
    public void ForceDeath()
    {
        if (!isDead)
        {
            currentHealth = 0f;
            Die();
        }
    }

    // Reset all buffs (useful for new game or testing)
    public void ResetBuffs()
    {
        damageBuffStacks = 0;
        cooldownBuffStacks = 0;
        movementBuffStacks = 0;
        attackSpeedBuffStacks = 0;
        highJumpBuffStacks = 0;
        ReapplyAllBuffs();
        Debug.Log("All buffs reset!");
    }

    // Backup respawn method that doesn't use coroutines
    private void RespawnPlayerDirect()
    {
        if (isRespawning) return;

        isRespawning = true;

        // Reset player state
        currentHealth = maxHealth;
        survivalTime = 0f;

        // Move player to respawn point
        Vector3 respawnPosition;
        if (respawnPoint != null)
        {
            respawnPosition = respawnPoint.position;
        }
        else
        {
            respawnPosition = Vector3.zero;
        }

        transform.position = respawnPosition;

        if (playerRigidbody != null)
        {
            playerRigidbody.position = new Vector2(respawnPosition.x, respawnPosition.y);
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        ReapplyAllBuffs();
        isDead = false;
        isRespawning = false;

        OnPlayerRespawn?.Invoke();
        Debug.Log("Player respawned (direct method)!");
    }
}