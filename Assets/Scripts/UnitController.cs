using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    public static UnitController selectedUnit;
    public TileControl manuelBaslangicTile;
    [Header("Birim Özellikleri")]
    public string unitName = "Birlik";
    public int maxMovementPoints = 4;
    public int currentMovementPoints;
    public int actionPoints = 1;

    [Header("Ayarlar")]
    public float moveSpeed = 5f;
    public LayerMask gridLayer;
    public LayerMask unitLayer;
    public GameObject selectionVisual;

    private Vector3 targetPosition;
    private TileControl currentTile;
    private bool isMoving = false;

    void Start()
    {
        currentMovementPoints = maxMovementPoints;
        targetPosition = transform.position;
        if (selectionVisual) selectionVisual.SetActive(false);
        Invoke("FindStartingTile", 0.2f);
    }

    void Update()
    {
        // 1. Týklama Algýlama
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleInput();
        }

        // 2. Hareket Gerçekleþtirme
        if (Vector3.Distance(transform.position, targetPosition) > 0.02f)
        {
            isMoving = true;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            isMoving = false;
        }
    }
    void HandleInput()
    {
        // 1. UI Kontrolü: Eðer bir butona týklýyorsan arkadaki karakter hareket etmesin
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 2. Fare Pozisyonundan Lazer Atma
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // KONTROL LOGU: Neye týkladýðýný konsolda görmeni saðlar
            Debug.Log($"<color=cyan>Týklanan:</color> {hit.collider.gameObject.name} | <color=yellow>Layer:</color> {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            // 3. TIKLANAN ÞEY BÝR ÜNÝTE MÝ? (Unit katmaný)
            if (((1 << hit.collider.gameObject.layer) & unitLayer) != 0)
            {
                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                if (clickedUnit != null)
                {
                    SelectThisUnit(clickedUnit);
                }
            }
            // 4. TIKLANAN ÞEY YER MÝ? (Grid katmaný)
            // Þartlar: Bu birim seçili olmalý, hareket etmiyor olmalý ve týklanan yer Grid olmalý
            else if (selectedUnit == this && !isMoving && ((1 << hit.collider.gameObject.layer) & gridLayer) != 0)
            {
                TileControl targetTile = hit.collider.GetComponent<TileControl>();

                if (targetTile != null)
                {
                    TryMove(targetTile);
                }
                else
                {
                    Debug.LogWarning("Týklanan objede TileControl scripti bulunamadý!");
                }
            }
        }
    }

    void SelectThisUnit(UnitController targetUnit)
    {
        // Eski seçili görselini kapat
        if (selectedUnit != null && selectedUnit.selectionVisual != null)
            selectedUnit.selectionVisual.SetActive(false);

        selectedUnit = targetUnit;

        // Yeni seçili görselini aç ve UI'ý güncelle
        if (selectedUnit != null)
        {
            if (selectedUnit.selectionVisual != null) selectedUnit.selectionVisual.SetActive(true);
            UIManager.Instance.ShowUnitInfo(selectedUnit);
        }
    }

    void TryMove(TileControl targetTile)
    {
        if (currentTile == null || targetTile == null)
        {
            Debug.LogWarning("Zemin verisi eksik!");
            return;
        }

        if (targetTile.isOccupied)
        {
            Debug.Log("Hedef kare dolu!");
            return;
        }

        // MESAFE HESABI
        float distanceX = Mathf.Abs(targetTile.transform.position.x - currentTile.transform.position.x);
        float distanceZ = Mathf.Abs(targetTile.transform.position.z - currentTile.transform.position.z);
        float totalDistance = distanceX + distanceZ;

        // KONSOLA MESAFEYÝ YAZDIR (Sorunu burada göreceðiz)
        Debug.Log($"<color=orange>Mesafe Hesabý:</color> Gereken: {totalDistance}, Sýnýr: 4.1f");

        // TEST ÝÇÝN: Sýnýrý 4.1f yerine 1000f yapalým. 
        // Eðer böyleyken hareket ederse, 4.1f senin haritan için çok küçük demektir.
        if (currentMovementPoints > 0 && totalDistance <= 1000f)
        {
            MoveTo(targetTile);
        }
        else
        {
            Debug.Log("Hareket puaný bitti veya çok uzak.");
        }
    }

    void MoveTo(TileControl targetTile)
    {
        if (currentTile != null) currentTile.isOccupied = false;

        currentTile = targetTile;
        currentTile.isOccupied = true;
        targetPosition = new Vector3(targetTile.transform.position.x, transform.position.y, targetTile.transform.position.z);

        currentMovementPoints--;
        UIManager.Instance.ShowUnitInfo(this); // UI Güncelle
    }



    void FindStartingTile()
    {
        // Lazer karakterin merkezinin 5 birim yukarýsýndan baþlasýn (Scale 5 olduðu için)
        Vector3 rayStart = transform.position + Vector3.up * 5f;

        // Yere doðru 50 birimlik uzun bir lazer atýyoruz
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, gridLayer))
        {
            currentTile = hit.collider.GetComponent<TileControl>();
            if (currentTile != null)
            {
                currentTile.isOccupied = true;
                // Karakteri Tile'ýn merkezine hizala
                targetPosition = new Vector3(currentTile.transform.position.x, transform.position.y, currentTile.transform.position.z);
                Debug.Log("<color=green>BAÞLANGIÇ TÝLE BULUNDU:</color> " + currentTile.name);
            }
        }
        else
        {
            Debug.LogError("KRÝTÝK HATA: Karakter baþlangýç tile'ýný bulamadý! Lütfen karakteri bir Tile'ýn tam üzerine koyun.");
        }
    }
}