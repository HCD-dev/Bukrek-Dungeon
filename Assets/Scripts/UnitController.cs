using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    private bool isWaitingForText = false;
    [HideInInspector] public bool isMovementModeActive = false;
    private EnemyController lastHoveredEnemy;
    public static UnitController selectedUnit;

    [Header("Birim Kimliði")]
    public string unitName;

    [Header("Stat Ayarlarý")]
    public int maxHealth = 100;
    public int currentHealth;
    public int attackPower = 20;
    public int dodgeChance = 0; // Dodge statý burada durmalý ama Text referansýna gerek yok

    [Space]
    public int maxMovementPoints = 3;
    public int currentMovementPoints;

    [Space]
    public int maxActionPoints = 1;
    public int currentActionPoints;

    public float moveSpeed = 8f;

    [Header("Grid Ayarlarý")]
    public float stepSize = 10f;

    [Header("Görsel")]
    public GameObject selectionRing; // Sadece halka referansýný býraktým

    private Vector3 targetPosition;
    private bool isMoving = false;
    [HideInInspector] public bool isSelectingTarget = false;

    [Header("Menzil Ayarlarý")]
    public int attackRange = 1;
    public int hitChance = 85;

    private TileControl lastHoveredTile;
    
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        targetPosition = transform.position;

        if (selectionRing != null) selectionRing.SetActive(false);
    }

    void Update()
    {
        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn) return;
        if (animator != null)
        {
            // isMoving deðiþkenini kullanarak animasyonu deðiþtir
            animator.SetBool("isMoving", isMoving);
        }
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

    // --- Gereksiz UpdateDodgeText ve UpdateMovementText fonksiyonlarýný sildik ---

    void HandleHover()
    {
        if (isWaitingForText) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (isSelectingTarget && selectedUnit == this)
            {
                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    if (lastHoveredEnemy != null && lastHoveredEnemy != enemy)
                        lastHoveredEnemy.ClearRangeText();

                    lastHoveredEnemy = enemy;

                    float dist = Vector3.Distance(transform.position, enemy.transform.position) / stepSize;
                    int distanceInTiles = Mathf.RoundToInt(dist);

                    if (distanceInTiles <= attackRange)
                    {
                        int finalChance = hitChance - enemy.dodgeChance;
                        enemy.SetRangeText("<color=green>In Range!</color> HitChance: %" + finalChance);
                    }
                    else
                    {
                        enemy.SetRangeText("<color=red>Out of Range!</color> Distance: " + distanceInTiles);
                    }
                    return;
                }
            }

            if (selectedUnit == this && !isMoving && isMovementModeActive)
            {
                TileControl tile = hit.collider.GetComponent<TileControl>();
                if (tile != null)
                {
                    if (lastHoveredTile == tile) return;
                    if (lastHoveredTile != null) lastHoveredTile.HideRange();

                    lastHoveredTile = tile;

                    float dist = Vector3.Distance(transform.position, tile.transform.position) / stepSize;
                    int totalSteps = Mathf.RoundToInt(dist);

                    bool canMove = totalSteps <= currentMovementPoints && totalSteps > 0 && !tile.isOccupied;
                    tile.ShowRange(canMove);
                    return;
                }
            }
        }
        ClearHovers();
    }

    private void ClearHovers()
    {
        if (lastHoveredEnemy != null)
        {
            lastHoveredEnemy.ClearRangeText();
            lastHoveredEnemy = null;
        }

        if (lastHoveredTile != null)
        {
            lastHoveredTile.HideRange();
            lastHoveredTile = null;
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (isSelectingTarget && selectedUnit == this)
                {
                    EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                    if (enemy != null && currentActionPoints > 0)
                    {
                        PerformAttack(enemy);
                    }
                    isSelectingTarget = false;
                    return;
                }

                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                if (clickedUnit != null)
                {
                    SelectUnit(clickedUnit);
                    return;
                }

                if (selectedUnit == this && currentMovementPoints > 0 && !isMoving && isMovementModeActive)
                {
                    MoveToTarget(hit.point);
                    isMovementModeActive = false;
                }
            }
        }
    }

    void MoveToTarget(Vector3 worldPosition)
    {
        float gridX = Mathf.Round(worldPosition.x / stepSize) * stepSize;
        float gridZ = Mathf.Round(worldPosition.z / stepSize) * stepSize;
        Vector3 finalTarget = new Vector3(gridX, transform.position.y, gridZ);

        Collider[] colliders = Physics.OverlapSphere(finalTarget, 1f);
        foreach (var col in colliders)
        {
            if (col.gameObject != this.gameObject && (col.GetComponent<UnitController>() || col.GetComponent<EnemyController>()))
            {
                return;
            }
        }

        float distanceX = Mathf.Abs(finalTarget.x - transform.position.x) / stepSize;
        float distanceZ = Mathf.Abs(finalTarget.z - transform.position.z) / stepSize;
        int totalSteps = Mathf.RoundToInt(distanceX + distanceZ);

        if (totalSteps > 0 && totalSteps <= currentMovementPoints)
        {
            if (lastHoveredTile != null) lastHoveredTile.HideRange();

            targetPosition = finalTarget;
            isMoving = true;
            currentMovementPoints -= totalSteps;

            // Bilgileri direkt UIManager üzerinden güncelliyoruz
            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        }
    }

    public void SelectUnit(UnitController unit)
    {
        UnitController[] allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (UnitController u in allUnits) u.DeselectUnit();

        selectedUnit = unit;
        if (unit.selectionRing != null) unit.selectionRing.SetActive(true);

        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(unit);
    }

    public void DeselectUnit()
    {
        isSelectingTarget = false;
        isMovementModeActive = false;
        if (selectionRing != null) selectionRing.SetActive(false);
        ClearHovers();
    }

    public void PerformAttack(EnemyController target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position) / stepSize;
        int distanceInTiles = Mathf.RoundToInt(dist);

        if (distanceInTiles <= attackRange)
        {
            isWaitingForText = true;
            currentActionPoints--;
            int finalHitChance = hitChance - target.dodgeChance;
            int randomRoll = Random.Range(1, 101);

            if (randomRoll <= finalHitChance)
            {
                target.TakeDamage(attackPower);
                target.SetRangeText("<size=120%><color=yellow>HIT!</color></size>");
            }
            else
            {
                target.SetRangeText("<size=120%><color=red>MISS!</color></size>");
            }

            StartCoroutine(ClearTextAfterDelay(target, 1.5f));

            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        }
    }

    private System.Collections.IEnumerator ClearTextAfterDelay(EnemyController target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null) target.ClearRangeText();
        isWaitingForText = false;
    }

    public void ResetPoints()
    {
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        isSelectingTarget = false;
        isMovementModeActive = false;
        isMoving = false;
    }
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(unitName + " hasar aldý! Kalan HP: " + currentHealth);

        // Paneli güncelle
        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);

        if (currentHealth <= 0)
        {
            Debug.Log(unitName + " yenildi!");
            // Buraya ölüm animasyonu veya sahneyi yeniden baþlatma gelebilir
        }
    }


}