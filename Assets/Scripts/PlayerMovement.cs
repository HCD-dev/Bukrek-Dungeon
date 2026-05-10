using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 10f; // Hareket hýzý
    private Vector3 targetPosition;
    public LayerMask gridLayer; // Sadece kareleri algýlamak için

    void Start()
    {
        // Baþlangýçta olduðu yerde kalsýn
        targetPosition = transform.position;
    }

    void Update()
    {
        // 1. Fare týklamasýný algýla (Sol Týk)
        if (Input.GetMouseButtonDown(0))
        {
            SetTargetPosition();
        }

        // 2. Capsule'u hedef noktaya yumuþakça hareket ettir
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    void SetTargetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Kameradan fareye bir ýþýn gönder, sadece "Grid" layer'ýna çarparsa çalýþ
        if (Physics.Raycast(ray, out hit, 100f, gridLayer))
        {
            // Týkladýðýmýz karenin tam merkezini alýyoruz
            // Y deðerini Capsule'un boyuna göre ayarlýyoruz (Y=1 Capsule'u zeminin üstünde tutar)
            targetPosition = new Vector3(hit.transform.position.x, 1f, hit.transform.position.z);
        }
    }
}