using UnityEngine;

public class TileControl : MonoBehaviour
{
    public bool isOccupied = false; // Kare dolu mu?

    [Header("Movement Visualization")]
    [Tooltip("Yeþil kare objesi (Gidebildiðimiz yerler)")]
    public GameObject greenOverlay;

    [Tooltip("Kýrmýzý kare objesi (Gidemediðimiz yerler)")]
    public GameObject redOverlay;

    [HideInInspector] public GameObject occupantUnit;

    void Awake()
    {
        // Oyun baþýnda her iki görseli de gizle (beyaz ana tile görünecek sadece)
        if (greenOverlay != null) greenOverlay.SetActive(false);
        if (redOverlay != null) redOverlay.SetActive(false);
    }

   

    public void ShowRange(bool canMove)
    {
        // canMove true ise yeþili aç kýrmýzýyý kapat, false ise tam tersi
        if (canMove)
        {
            if (greenOverlay != null) greenOverlay.SetActive(true);
            if (redOverlay != null) redOverlay.SetActive(false);
        }
        else
        {
            if (greenOverlay != null) greenOverlay.SetActive(false);
            if (redOverlay != null) redOverlay.SetActive(true);
        }
    }

    public void HideRange()
    {
        // Mouse üzerinden çekilince her ikisini de kapat, ana beyaz tile kalsýn
        if (greenOverlay != null) greenOverlay.SetActive(false);
        if (redOverlay != null) redOverlay.SetActive(false);
    }

    // --- Gizmo Ayarlarý (Dolu kareleri editörde görmek için) ---

    private void OnDrawGizmos()
    {
        if (isOccupied)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.2f);
        }
    }
}