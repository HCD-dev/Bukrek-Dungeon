using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

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
    public int dodgeChance = 0;

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
    public GameObject selectionRing;

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

        SetTileOccupied(transform.position, true);
    }

    void Update()
    {
        if (TurnManager.Instance.currentState != TurnManager.TurnState.PlayerTurn) return;

        if (animator != null)
            animator.SetBool("isMoving", isMoving);

        HandleInput();
        HandleHover();

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                transform.position = targetPosition;
                isMoving = false;
                SetTileOccupied(transform.position, true);
            }
        }
    }

    public LayerMask gridLayer;

    private void SetTileOccupied(Vector3 pos, bool occupied)
    {
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 10f, gridLayer))
        {
            TileControl tile = hit.collider.GetComponent<TileControl>();
            if (tile != null)
            {
                tile.isOccupied = occupied;
            }
        }
    }

    private TileControl GetTileAtPosition(Vector3 pos)
    {
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 10f, gridLayer))
        {
            return hit.collider.GetComponent<TileControl>();
        }
        return null;
    }

    bool IsPathClear(Vector3 start, Vector3 end)
    {
        Vector3 gridStart = new Vector3(
            Mathf.Round(start.x / stepSize) * stepSize,
            start.y,
            Mathf.Round(start.z / stepSize) * stepSize
        );

        Vector3 gridEnd = new Vector3(
            Mathf.Round(end.x / stepSize) * stepSize,
            start.y,
            Mathf.Round(end.z / stepSize) * stepSize
        );

        int startGridX = Mathf.RoundToInt(gridStart.x / stepSize);
        int startGridZ = Mathf.RoundToInt(gridStart.z / stepSize);
        int endGridX = Mathf.RoundToInt(gridEnd.x / stepSize);
        int endGridZ = Mathf.RoundToInt(gridEnd.z / stepSize);

        int stepX = startGridX == endGridX ? 0 : (endGridX > startGridX ? 1 : -1);
        int stepZ = startGridZ == endGridZ ? 0 : (endGridZ > startGridZ ? 1 : -1);

        int currentX = startGridX;
        int currentZ = startGridZ;

        while (currentX != endGridX || currentZ != endGridZ)
        {
            // Hedef hariç tüm tile'larý kontrol et
            if (currentX != endGridX) currentX += stepX;
            if (currentZ != endGridZ) currentZ += stepZ;

            Vector3 checkPos = new Vector3(currentX * stepSize, start.y, currentZ * stepSize);
            TileControl tile = GetTileAtPosition(checkPos);

            if (tile != null && tile.isOccupied && !(currentX == endGridX && currentZ == endGridZ))
            {
                return false;
            }
        }

        // Hedef tile kontrol
        TileControl targetTile = GetTileAtPosition(gridEnd);
        if (targetTile != null && targetTile.isOccupied)
            return false;

        return true;
    }

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

                    bool canMove = totalSteps <= currentMovementPoints && totalSteps > 0 && !tile.isOccupied && IsPathClear(transform.position, tile.transform.position);
                    tile.ShowRange(canMove);
                    return;
                }
            }
        }
        ClearHovers();
    }

    private void ClearHovers()
    {
        if (lastHoveredEnemy != null) { lastHoveredEnemy.ClearRangeText(); lastHoveredEnemy = null; }
        if (lastHoveredTile != null) { lastHoveredTile.HideRange(); lastHoveredTile = null; }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (selectedUnit != this)
                {
                    UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                    if (clickedUnit != null) SelectUnit(clickedUnit);
                    return;
                }

                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null && isSelectingTarget)
                {
                    PerformAttack(enemy);
                }
                else if (hit.collider.GetComponent<TileControl>() && isMovementModeActive)
                {
                    if (IsPathClear(transform.position, hit.collider.transform.position))
                    {
                        MoveToTarget(hit.collider.transform.position);
                    }
                }
            }
        }
    }

    void MoveToTarget(Vector3 worldPosition)
    {
        float gridX = Mathf.Round(worldPosition.x / stepSize) * stepSize;
        float gridZ = Mathf.Round(worldPosition.z / stepSize) * stepSize;
        Vector3 finalTarget = new Vector3(gridX, transform.position.y, gridZ);

        float distanceX = Mathf.Abs(finalTarget.x - transform.position.x) / stepSize;
        float distanceZ = Mathf.Abs(finalTarget.z - transform.position.z) / stepSize;
        int totalSteps = Mathf.RoundToInt(distanceX + distanceZ);

        if (totalSteps > 0 && totalSteps <= currentMovementPoints)
        {
            SetTileOccupied(transform.position, false);

            if (lastHoveredTile != null) lastHoveredTile.HideRange();
            targetPosition = finalTarget;
            isMoving = true;
            currentMovementPoints -= totalSteps;

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
        if (currentActionPoints <= 0) return;

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

    private IEnumerator ClearTextAfterDelay(EnemyController target, float delay)
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
        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        SetTileOccupied(transform.position, false);
        Destroy(gameObject);
    }
}