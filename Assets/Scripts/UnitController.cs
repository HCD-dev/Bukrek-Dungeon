using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class UnitController : MonoBehaviour
{
    private bool isInteractionLocked = false;
    [HideInInspector] public bool isMovementModeActive = false;
    private EnemyController lastHoveredEnemy;
    public static UnitController selectedUnit;

    [Header("Identity")]
    public string unitName;

    [Header("Vitals")]
    public int maxHealth = 100;
    public int currentHealth;
    public int attackPower = 20;
    public int dodgeChance = 0;

    [Header("Movement & Actions")]
    public int maxMovementPoints = 3;
    public int currentMovementPoints;
    public int maxActionPoints = 1;
    public int currentActionPoints;
    public float moveSpeed = 8f;

    [Header("Grid Config")]
    public float stepSize = 10f;
    public LayerMask gridLayer;
    public GameObject selectionRing;

    private Vector3 targetPosition;
    private bool isMoving = false;
    [HideInInspector] public bool isSelectingTarget = false;

    [Header("Combat Settings")]
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
        SetTileStatus(transform.position, true);
    }

    void Update()
    {
        // TurnManager isim deðiþikliklerine göre güncellenen kýsým
        if (TurnManager.Instance.CurrentPhase != TurnManager.TurnPhase.Player) return;

        if (animator != null)
            animator.SetBool("isMoving", isMoving);

        HandleInteraction();
        ProcessHoverEffects();

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                SetTileStatus(transform.position, true);
            }
        }
    }

    private void SetTileStatus(Vector3 pos, bool occupied)
    {
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 10f, gridLayer))
        {
            TileControl tile = hit.collider.GetComponent<TileControl>();
            if (tile != null) tile.isOccupied = occupied;
        }
    }

    private TileControl LocateTile(Vector3 pos)
    {
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 10f, gridLayer))
        {
            return hit.collider.GetComponent<TileControl>();
        }
        return null;
    }

    private bool CheckUnitPresence(Vector3 pos)
    {
        float radius = stepSize * 0.4f;
        Collider[] hits = Physics.OverlapSphere(new Vector3(pos.x, pos.y + 0.5f, pos.z), radius);
        foreach (var col in hits)
        {
            if (col.GetComponent<UnitController>() != null || col.GetComponent<EnemyController>() != null)
                return true;
        }
        return false;
    }

    bool ValidatePath(Vector3 start, Vector3 end)
    {
        int startX = Mathf.RoundToInt(start.x / stepSize);
        int startZ = Mathf.RoundToInt(start.z / stepSize);
        int endX = Mathf.RoundToInt(end.x / stepSize);
        int endZ = Mathf.RoundToInt(end.z / stepSize);

        int dirX = (endX > startX) ? 1 : (endX < startX ? -1 : 0);
        int dirZ = (endZ > startZ) ? 1 : (endZ < startZ ? -1 : 0);

        int currX = startX;
        int currZ = startZ;

        while (currX != endX || currZ != endZ)
        {
            if (currX != endX) currX += dirX;
            if (currZ != endZ) currZ += dirZ;

            if (currX == endX && currZ == endZ) break;

            Vector3 checkPos = new Vector3(currX * stepSize, start.y, currZ * stepSize);
            TileControl tile = LocateTile(checkPos);
            if ((tile != null && tile.isOccupied) || CheckUnitPresence(checkPos)) return false;
        }

        TileControl targetTile = LocateTile(new Vector3(endX * stepSize, start.y, endZ * stepSize));
        return targetTile != null && !targetTile.isOccupied && !CheckUnitPresence(end);
    }

    void ProcessHoverEffects()
    {
        if (isInteractionLocked) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            if (isSelectingTarget && selectedUnit == this)
            {
                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    if (lastHoveredEnemy != null && lastHoveredEnemy != enemy) lastHoveredEnemy.ClearRangeText();
                    lastHoveredEnemy = enemy;

                    int dist = Mathf.RoundToInt(Vector3.Distance(transform.position, enemy.transform.position) / stepSize);
                    if (dist <= attackRange)
                        enemy.SetRangeText($"<color=green>READY</color>\nHit: %{hitChance - enemy.dodgeChance}");
                    else
                        enemy.SetRangeText($"<color=red>OUT OF RANGE</color>\nDist: {dist}");
                    return;
                }
            }

            if (selectedUnit == this && !isMoving && isMovementModeActive)
            {
                TileControl tile = hit.collider.GetComponent<TileControl>();
                if (tile != null && lastHoveredTile != tile)
                {
                    if (lastHoveredTile != null) lastHoveredTile.HideRange();
                    lastHoveredTile = tile;

                    int dist = Mathf.RoundToInt(Vector3.Distance(transform.position, tile.transform.position) / stepSize);
                    bool isValid = dist <= currentMovementPoints && dist > 0 && ValidatePath(transform.position, tile.transform.position);
                    tile.ShowRange(isValid);
                    return;
                }
            }
        }
        ResetHoverVisuals();
    }

    private void ResetHoverVisuals()
    {
        if (lastHoveredEnemy != null) { lastHoveredEnemy.ClearRangeText(); lastHoveredEnemy = null; }
        if (lastHoveredTile != null) { lastHoveredTile.HideRange(); lastHoveredTile = null; }
    }

    void HandleInteraction()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (selectedUnit != this)
                {
                    UnitController targetUnit = hit.collider.GetComponent<UnitController>();
                    if (targetUnit != null) FocusOnUnit(targetUnit);
                    return;
                }

                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null && isSelectingTarget) ExecuteCombat(enemy);
                else if (hit.collider.GetComponent<TileControl>() && isMovementModeActive)
                {
                    if (ValidatePath(transform.position, hit.collider.transform.position))
                        InitiateMove(hit.collider.transform.position);
                }
            }
        }
    }

    void InitiateMove(Vector3 destination)
    {
        float targetX = Mathf.Round(destination.x / stepSize) * stepSize;
        float targetZ = Mathf.Round(destination.z / stepSize) * stepSize;
        Vector3 finalCoords = new Vector3(targetX, transform.position.y, targetZ);

        int cost = Mathf.RoundToInt((Mathf.Abs(finalCoords.x - transform.position.x) + Mathf.Abs(finalCoords.z - transform.position.z)) / stepSize);

        if (cost > 0 && cost <= currentMovementPoints && !CheckUnitPresence(finalCoords))
        {
            SetTileStatus(transform.position, false);
            if (lastHoveredTile != null) lastHoveredTile.HideRange();

            targetPosition = finalCoords;
            isMoving = true;
            currentMovementPoints -= cost;

            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        }
    }

    public void FocusOnUnit(UnitController unit)
    {
        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (var u in units) u.Deselect();
        selectedUnit = unit;
        if (unit.selectionRing != null) unit.selectionRing.SetActive(true);
        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(unit);
    }

    public void Deselect()
    {
        isSelectingTarget = false;
        isMovementModeActive = false;
        if (selectionRing != null) selectionRing.SetActive(false);
        ResetHoverVisuals();
    }

    public void ExecuteCombat(EnemyController target)
    {
        if (currentActionPoints <= 0) return;

        int dist = Mathf.RoundToInt(Vector3.Distance(transform.position, target.transform.position) / stepSize);
        if (dist <= attackRange)
        {
            isInteractionLocked = true;
            currentActionPoints--;

            if (Random.Range(1, 101) <= (hitChance - target.dodgeChance))
            {
                target.TakeDamage(attackPower);
                target.SetRangeText("<color=yellow>HIT!</color>");
            }
            else target.SetRangeText("<color=red>MISS</color>");

            StartCoroutine(ReleaseInteractionLock(target, 1.2f));
            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        }
    }

    private IEnumerator ReleaseInteractionLock(EnemyController target, float time)
    {
        yield return new WaitForSeconds(time);
        if (target != null) target.ClearRangeText();
        isInteractionLocked = false;
    }

    public void RefreshUnit()
    {
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        isSelectingTarget = false;
        isMovementModeActive = false;
        isMoving = false;
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        if (currentHealth <= 0) HandleDeath();
    }

    void HandleDeath()
    {
        SetTileStatus(transform.position, false);
        Destroy(gameObject);
    }
}