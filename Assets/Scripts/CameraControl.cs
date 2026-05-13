using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Senin Harita Koordinatlarýn")]
    // Oyun baþlar baþlamaz kameranýn gideceði yer
    public Vector3 startPosition = new Vector3(250f, 30f, 90f);

    [Header("Hareket Ayarlarý")]
    public float moveSpeed = 40f;

    [Header("Sýnýrlar (Burasý Çok Önemli!)")]
    // Haritan 250'de olduðu için sýnýrlarý buna göre çok geniþ tutuyoruz
    public float minX = 0f;
    public float maxX = 500f;    // 50 olan yeri 500 yaptýk, artýk 250'ye izin verir
    public float minZ = 0f;
    public float maxZ = 300f;    // 90 olan yeri kapsasýn diye 300 yaptýk

    [Header("Zoom")]
    public float scrollSpeed = 600f;
    public float minY = 5f;
    public float maxY = 100f;

    void Start()
    {
        // Kamera senin haritana ýþýnlansýn
        transform.position = startPosition;

        // Bakýþ açýsý için (Ýsteðe baðlý, elinle de ayarlayabilirsin)
        transform.rotation = Quaternion.Euler(45, 0, 0);
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        float horizontal = 0;
        float vertical = 0;

        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;

        Vector3 moveVector = new Vector3(horizontal, 0, vertical);
        transform.position += moveVector.normalized * moveSpeed * Time.deltaTime;

        // --- SINIRLAMA (KLAMP) ---
        // Artýk 250 ve 90 bu aralýkta (0 ile 500 arasý) olduðu için kamera kaçmayacak.
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);

        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 newPos = transform.position + (transform.forward * scroll * scrollSpeed);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 10f);
        }
    }
}