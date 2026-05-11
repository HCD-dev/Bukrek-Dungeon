using UnityEngine;

public class TileControl : MonoBehaviour
{
    public bool isOccupied = false; // Kare dolu mu?

    // Opsiyonel: Bu kare üzerinde duran birimi de tutabiliriz
    [HideInInspector] public GameObject occupantUnit;

    // Editörde hangi karelerin dolu olduðunu "Gizmo" ile görebilirsin
    // Bu sadece Scene ekranýnda görünür, oyunda görünmez.
    private void OnDrawGizmos()
    {
        if (isOccupied)
        {
            Gizmos.color = Color.red;
            // Karenin üzerinde küçük bir kýrmýzý küre çizer
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.2f);
        }
    }
}