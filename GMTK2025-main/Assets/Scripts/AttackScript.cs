using Unity.VisualScripting;
using UnityEngine;

public class AttackScript : MonoBehaviour
{
    [Header("Aerial Strike Settings")]
    public float AerialStrike_Cooldown = 1f;
    public float AerialStrike_Duration = 1f; // Delay before shooting
    public GameObject AerialStrike_GameObject;
    public float spawnHeight = 5f;
    public Transform player;

    private bool canUseAerialStrike = true;

    private void Update()
    {
        TryAerialStrike();
    }

    public void TryAerialStrike()
    {
        if (canUseAerialStrike)
        {
            StartCoroutine(AerialStrikeRoutine());
        }
    }

    private System.Collections.IEnumerator AerialStrikeRoutine()
    {
        canUseAerialStrike = false;

        // 1. Spawn projectile above player
        Vector3 spawnPosition = player.position + Vector3.up * spawnHeight;
        GameObject projectile = Instantiate(AerialStrike_GameObject, spawnPosition, Quaternion.identity);

        Debug.Log("Aerial Strike: Spawned above player!");

        // 2. Wait duration before shooting
        yield return new WaitForSeconds(AerialStrike_Duration);

        // 3. Make it shoot downward - but check if projectile still exists
        if (projectile != null)
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.down * 10f;
                Debug.Log("Aerial Strike: Fired downward!");
            }
            else
            {
                Debug.LogWarning("Projectile needs a Rigidbody to move!");
            }

            // Destroy projectile after it fires (with delay)
            Destroy(projectile, 2f);
        }
        else
        {
            Debug.LogWarning("Projectile was destroyed before it could fire!");
        }

        yield return new WaitForSeconds(AerialStrike_Cooldown);
        canUseAerialStrike = true;

        Debug.Log("Aerial Strike ready again!");
    }
}