using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager instance;
    public float bossMaxHealth = 100f;
    public SpriteRenderer bossSprite;
    [SerializeField] float bossCurrentHealth = 0f;

    Color bossColor;
    private void Awake()
    {
        bossCurrentHealth = bossMaxHealth;
        instance = this;
    }

    private void Start()
    {
        bossSprite.color = bossColor;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("boss got HIT!");

        bossCurrentHealth -= damage;
        bossSprite.color = Color.red;
        // Clamp so it doesn't go below zero
        bossCurrentHealth = Mathf.Max(bossCurrentHealth, 0f);

        Debug.Log($"Boss HP: {bossCurrentHealth}/{bossMaxHealth}");

        StopAllCoroutines();
        StartCoroutine(FlashDamage());


        if (bossCurrentHealth <= 0f)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashDamage()
    {
        bossSprite.color = Color.red;

        // Wait a short moment
        yield return new WaitForSeconds(0.1f);

        // Smoothly go back to original color
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            bossSprite.color = Color.Lerp(Color.red, bossColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        bossSprite.color = bossColor;
    }

    private void Die()
    {
        Debug.Log("Boss defeated!");
        // Add your boss defeat logic here (animation, drop loot, etc)
    }

    public float GetCurrentHealth()
    {
        return bossCurrentHealth;
    }
}
