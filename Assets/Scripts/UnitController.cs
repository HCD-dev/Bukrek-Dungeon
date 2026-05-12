using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    public static UnitController selectedUnit;



    [Header("Birim Kimliði")]
    public string unitName;

    [Header("Stat Ayarlarý (Inspector)")]
    public int maxHealth = 100;
    public int currentHealth;
    public int attackPower = 20;

    [Space]
    public int maxMovementPoints = 3; // Toplam hareket hakký
    public int currentMovementPoints;

    [Space]
    public int maxActionPoints = 1;   // Saldýrý hakký
    public int currentActionPoints;

    public float moveSpeed = 8f;

    [Header("Grid Ayarlarý (Senin Tile Ölçülerin)")]
    public float tileWidth = 0.9f;   // Tile Scale X
    public float tileLength = 0.9f;  // Tile Scale Z

    [Header("UI ve Görsel")]
    public GameObject selectionRing;
    public TextMeshProUGUI movementText;

    private Vector3 targetPosition;
    private bool isMoving = false;
    [HideInInspector] public bool isSelectingTarget = false;

    [Header("Menzil Ayarlarý")]
    public int attackRange = 1; // Erlik için 1, Mergen için 5 yaparsýn
    public TextMeshProUGUI rangeStatusText;

    public int hitChance = 85;
    public int rangeD = 1;
    
    void Start()
    {
        currentHealth = maxHealth;
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        targetPosition = transform.position;

        if (selectionRing != null) selectionRing.SetActive(false);
        if (movementText != null) movementText.text = "";
    }

    void Update()
    {
        HandleInput();
        HandleHover();

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }
    void HandleHover()
    {
        if (selectedUnit == this && isSelectingTarget)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position) / 10f;
                    int distanceInTiles = Mathf.RoundToInt(dist);

                    if (distanceInTiles <= attackRange)
                    {
                        // Buradaki %90 yerine artýk ünitenin kendi hitChance deðerini yazýyoruz
                        rangeStatusText.text = "<color=green>Menzil Ýçinde</color> Ýsabet Þansý: %" + hitChance;
                    }
                    else
                    {
                        rangeStatusText.text = "<color=red>Menzil Dýþýnda</color> Uzaklýk: " + distanceInTiles;
                    }
                    return;
                }
            }
        }
        if (rangeStatusText != null && rangeStatusText.text != "") rangeStatusText.text = "";
    }
    void HandleInput()
    {
        
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject()) 
                {
                    return;
                }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 1. SALDIRI MODU
                if (isSelectingTarget)
                {
                    EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                    if (enemy != null && currentActionPoints > 0)
                    {
                        PerformAttack(enemy);
                    }
                    isSelectingTarget = false;
                    return;
                }

                // 2. ÜNÝTE SEÇME
                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                if (clickedUnit != null)
                {
                    SelectUnit(clickedUnit);
                    return;
                }

                // 3. HAREKET (Hesaplamalý)
                if (selectedUnit == this && currentMovementPoints > 0 && !isMoving)
                {
                    MoveToTarget(hit.point);
                }
            }
        
            }
    }

    void MoveToTarget(Vector3 worldPosition)
    {
        float stepSize = 10f;
        float gridX = Mathf.Round(worldPosition.x / stepSize) * stepSize;
        float gridZ = Mathf.Round(worldPosition.z / stepSize) * stepSize;

        // Hedeflenen pozisyonu belirle
        Vector3 finalTarget = new Vector3(gridX, transform.position.y, gridZ);

        // --- DÜZELTME: KENDÝNE ÇARPMAMA KONTROLÜ ---
        // Hedef noktada baþka collider var mý bakýyoruz.
        Collider[] colliders = Physics.OverlapSphere(finalTarget, 1f);
        foreach (var col in colliders)
        {
            // Eðer çarptýðýn þey bu ünitenin kendisi DEÐÝLSE ve bir karakterse
            if (col.gameObject != this.gameObject && (col.GetComponent<UnitController>() || col.GetComponent<EnemyController>()))
            {
                Debug.Log("<color=red>Hata: Tile dolu! Engel: " + col.gameObject.name + "</color>");
                return;
            }
        }
        // ------------------------------------------

        float distanceX = Mathf.Abs(finalTarget.x - transform.position.x) / stepSize;
        float distanceZ = Mathf.Abs(finalTarget.z - transform.position.z) / stepSize;
        int totalSteps = Mathf.RoundToInt(distanceX + distanceZ);

        if (totalSteps > 0 && totalSteps <= currentMovementPoints)
        {
            targetPosition = finalTarget;
            isMoving = true;
            currentMovementPoints -= totalSteps;

            UpdateMovementText();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowUnitInfo(this);

            Debug.Log("<color=green>Hareket Onaylandý!</color>");
        }
        else if (totalSteps == 0)
        {
            Debug.Log("Zaten buradasýn.");
        }
        else
        {
            Debug.Log("<color=red>Menzil Dýþý!</color> Gereken: " + totalSteps + " Mevcut: " + currentMovementPoints);
        }
    }
    public void SelectUnit(UnitController unit)
    {
        UnitController[] allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (UnitController u in allUnits) u.DeselectUnit();

        selectedUnit = unit;
        if (unit.selectionRing != null) unit.selectionRing.SetActive(true);

        UIManager.Instance.ShowUnitInfo(unit);
        unit.UpdateMovementText();
    }

    public void DeselectUnit()
    {
        if (selectionRing != null) selectionRing.SetActive(false);
        if (movementText != null) movementText.text = "";
    }

    public void UpdateMovementText()
    {
        if (movementText != null && selectedUnit == this)
            movementText.text = "MP: " + currentMovementPoints;
    }

    public void PerformAttack(EnemyController target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position) / 10f;
        int distanceInTiles = Mathf.RoundToInt(dist);

        if (distanceInTiles <= attackRange)
        {
            currentActionPoints--; // Saldýrý giriþimi yapýldýðý için puan düþer

            // --- ÝSABET KONTROLÜ (Zar Atma) ---
            int randomRoll = Random.Range(1, 101); // 1 ile 100 arasý sayý tut

            if (randomRoll <= hitChance)
            {
                // ÝSABET!
                target.TakeDamage(attackPower);
                Debug.Log("<color=green>ÝSABET!</color> " + unitName + " vurdu. Zar: " + randomRoll + " / " + hitChance);
            }
            else
            {
                // ISKALADI!
                Debug.Log("<color=orange>ISKALADI!</color> " + unitName + " hedefi tutturamadý. Zar: " + randomRoll + " / " + hitChance);
            }

            UIManager.Instance.ShowUnitInfo(this);
            UpdateMovementText();
        }
        else
        {
            Debug.Log("Çok uzak! Saldýramazsýn.");
        }
    }

    public void StartTargetSelection()
{
    if (currentActionPoints > 0)
    {
        isSelectingTarget = true;
        // Ýstersen burada imleci deðiþtirebilirsin
        Debug.Log("Saldýrý Modu Aktif. Bir düþmana týkla!");
    }
}

    public void ResetPoints()
    {
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        isSelectingTarget = false;
        isMoving = false;
    }
}