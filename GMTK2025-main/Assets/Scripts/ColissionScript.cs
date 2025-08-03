using Unity.VisualScripting;
using UnityEngine;

public class ColissionScript : MonoBehaviour
{

    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        int otherLayer = other.layer;
        if (otherLayer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log("Hit the ground!");
            Destroy(gameObject);
        }
    }
}
