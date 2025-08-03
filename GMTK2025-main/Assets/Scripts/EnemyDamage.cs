using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Enemy Damage Settings")]
    public float damageAmount = 20f;
    public float damageInterval = 1f; // Time between damage ticks for continuous contact

    private float lastDamageTime = 0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DealDamageToPlayer();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Deal damage over time if player stays in contact
            if (Time.time - lastDamageTime >= damageInterval)
            {
                DealDamageToPlayer();
            }
        }
    }

    private void DealDamageToPlayer()
    {
        if (hitScript.instance != null && !hitScript.instance.IsInvulnerable())
        {
            hitScript.instance.TakeDamageFromEnemy(damageAmount);
            lastDamageTime = Time.time;
            Debug.Log($"Enemy dealt {damageAmount} damage to player!");
        }
    }
}