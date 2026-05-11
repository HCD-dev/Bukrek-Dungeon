using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public class UnitController : MonoBehaviour
{
    public static UnitController selectedUnit;
    public TileControl manuelBaslangicTile;

    [Header("Birim Özellikleri")]
    public string unitName = "Birlik";
    public int maxMovementPoints = 4;
    public int currentMovementPoints;
    public int actionPoints = 1;
    public TextMeshProUGUI movementText;
    public TextMeshProUGUI actionText; 
    public TextMeshProUGUI nameText;

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

        // Zemin oluþumu için bekletiyoruz
        Invoke("FindStartingTile", 0.5f);
        UpdateMovementText();
    }

    public void UpdateMovementText()
    {
        // 1. Basit Movement: 5 yazýsý için
        if (movementText != null)
        {
            movementText.text = "Movement: " + currentMovementPoints;
        }

        // 2. UIManager üzerindeki "Hareket: 5 / 5" kýsmýný tazelemek için
        if (UIManager.Instance != null && selectedUnit == this)
        {
            UIManager.Instance.ShowUnitInfo(this);
        }
    }

    void Update()
    {
        // 1. Týklama Algýlama
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleInput();
        }

        // 2. Hareket Gerçekleþtirme
        if (Vector3.Distance(transform.position, targetPosition) > 0.05f)
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
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // Ünite seçimi
            if (((1 << hit.collider.gameObject.layer) & unitLayer) != 0)
            {
                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                if (clickedUnit != null) SelectThisUnit(clickedUnit);
            }
            // Hareket hedefi seçimi
            else if (selectedUnit == this && !isMoving && ((1 << hit.collider.gameObject.layer) & gridLayer) != 0)
            {
                TileControl targetTile = hit.collider.GetComponent<TileControl>();
                if (targetTile != null) TryMove(targetTile);
            }
        }
    }

    void SelectThisUnit(UnitController targetUnit)
    {
        if (selectedUnit != null && selectedUnit.selectionVisual != null)
            selectedUnit.selectionVisual.SetActive(false);

        selectedUnit = targetUnit;

        if (selectedUnit != null)
        {
            if (selectedUnit.selectionVisual != null) selectedUnit.selectionVisual.SetActive(true);
            // UIManager Instance kontrolü
            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(selectedUnit);
            selectedUnit.UpdateMovementText();
        }
    }

    void TryMove(TileControl targetTile)
    {
        if (currentTile == null || targetTile == null || targetTile.isOccupied) return;

        float diffX = Mathf.Abs(targetTile.transform.position.x - currentTile.transform.position.x);
        float diffZ = Mathf.Abs(targetTile.transform.position.z - currentTile.transform.position.z);

        // Mesafe hesabýna göre puan belirle
        int requiredPoints = Mathf.RoundToInt((diffX + diffZ) / 10f);

        if (currentMovementPoints >= requiredPoints && requiredPoints > 0)
        {
            currentMovementPoints -= requiredPoints;

            // ÖNEMLÝ: Hareket baþlamadan önce her iki texti de günceller
            UpdateMovementText();

            MoveTo(targetTile);
        }
        else
        {
            Debug.Log("Yetersiz hareket puaný!");
        }
    }

    void MoveTo(TileControl targetTile)
    {
        if (currentTile != null) currentTile.isOccupied = false;

        currentTile = targetTile;
        currentTile.isOccupied = true;

        targetPosition = new Vector3(targetTile.transform.position.x, transform.position.y, targetTile.transform.position.z);

        // Hareket bittiðinde veya baþladýðýnda UI'ý tekrar tetikle (Garanti olsun)
        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
    }

    void FindStartingTile()
    {
        Vector3 rayStart = transform.position + Vector3.up * 5f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, gridLayer))
        {
            currentTile = hit.collider.GetComponent<TileControl>();
            if (currentTile != null)
            {
                currentTile.isOccupied = true;
                targetPosition = new Vector3(currentTile.transform.position.x, transform.position.y, currentTile.transform.position.z);
                UpdateMovementText();
                Debug.Log("<color=green>BAÞLANGIÇ TÝLE BULUNDU:</color> " + currentTile.name);
            }
        }
        else
        {
            Debug.LogError("KRÝTÝK HATA: Karakter baþlangýç tile'ýný bulamadý!");
        }
    }
}