using UnityEngine;

public class hitScript : MonoBehaviour
{
    public static hitScript instance;
    public float playerDamage = 10f;

    [Header("Player Damage Reception")]
    public float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;

    private void Awake()
    {
        instance = this;
    }

    // For dealing damage to enemies
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            BossManager.instance.TakeDamage(playerDamage);
            Debug.Log($"Player dealt {playerDamage} damage to enemy!");
        }
    }

    // For receiving damage from enemies (call this method from enemy scripts)
    public void TakeDamageFromEnemy(float damage)
    {
        if (!isInvulnerable && PlayerDeathBuffSystem.instance != null)
        {
            PlayerDeathBuffSystem.instance.TakeDamage(damage);
            StartCoroutine(InvulnerabilityFrames());
        }
    }

    private System.Collections.IEnumerator InvulnerabilityFrames()
    {
        isInvulnerable = true;

        // Optional: Flash player sprite during invulnerability
        SpriteRenderer playerSprite = GetComponentInParent<SpriteRenderer>();
        if (playerSprite != null)
        {
            Color originalColor = playerSprite.color;

            float flashDuration = invulnerabilityDuration;
            float flashInterval = 0.1f;
            float elapsed = 0f;

            while (elapsed < flashDuration)
            {
                playerSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
                yield return new WaitForSeconds(flashInterval);
                playerSprite.color = originalColor;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval * 2;
            }

            playerSprite.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(invulnerabilityDuration);
        }

        isInvulnerable = false;
    }

    public bool IsInvulnerable() => isInvulnerable;
}