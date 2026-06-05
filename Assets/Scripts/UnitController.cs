using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using KevinIglesias;

public class UnitController : UnitBase
{
    public static UnitController selectedUnit;
    private bool isInteractionLocked = false;
    [HideInInspector] public bool isMovementModeActive = false;
    [HideInInspector] public bool isSelectingTarget = false;

    [Header("Player Specific Action Points")]
    public int maxMovementPoints = 3;
    public int currentMovementPoints;
    public int maxActionPoints = 1;
    public int currentActionPoints;

    [Header("Visuals")]
    public GameObject selectionRing;

    private EnemyController lastHoveredEnemy;
    private TileControl lastHoveredTile;
    private HumanArcherController archerCtrl;

    // Oyuncu özel eventleri
    public static event Action<UnitController> OnUnitSelected;
    public static event Action<UnitController> OnUnitActionPointsChanged;

    protected override void Start()
    {
        base.Start();
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;

        if (unitName == "Mergen")
        {
            archerCtrl = GetComponentInChildren<HumanArcherController>(true);
            if (archerCtrl != null) archerCtrl.gameObject.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        if (selectionRing != null) selectionRing.SetActive(false);
    }

    protected override void Update()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentPhase != TurnManager.TurnPhase.Player) return;

        base.Update(); // Ata sýnýftaki yürüme animasyonunu çalýþtýrýr.

        HandleInteraction();
        ProcessHoverEffects();

        if (isMoving)
        {
            ExecuteMovementPhysics();
        }
    }

    private void ExecuteMovementPhysics()
    {
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        if (moveDirection != Vector3.zero)
        {
            moveDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
            UpdateGridStatus(transform.position, true);
        }
    }

    public void ExecuteCombat(EnemyController target)
    {
        if (currentActionPoints <= 0 || isInteractionLocked) return;

        int dist = Mathf.RoundToInt(Vector3.Distance(transform.position, target.transform.position) / stepSize);
        if (dist <= attackRange)
        {
            isInteractionLocked = true;
            currentActionPoints--;
            OnUnitActionPointsChanged?.Invoke(this);

            transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));

            // Karakter özel saldýrý animasyonlarý
            if (unitName == "Mergen" && archerCtrl != null)
            {
                archerCtrl.PlayArcherAnimation(ArcherAnimation.ShootRunning);
                archerCtrl.LoadBow(0f, 0.4f);
                archerCtrl.ShootArrow(0.4f, 0.05f);
                archerCtrl.GetArrow(0.9f);
            }
            else if (animator != null && HasParameter("Attack"))
            {
                animator.SetTrigger("Attack");
            }

            // Ýsabet Kontrolü
            if (UnityEngine.Random.Range(1, 101) <= (hitChance - target.dodgeChance))
            {
                StartCoroutine(DelayedDamage(target, attackPower, 0.6f));
            }
            else
            {
                StartCoroutine(DelayedMiss(target, 0.6f));
            }

            StartCoroutine(ReleaseInteractionLock(target, 1.5f));
        }
    }

    // ÇAKIÞMA HATALARI DÜZELTÝLMÝÞ ETKÝLEÞÝM MANTIÐI
    private void HandleInteraction()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit; // Deðiþken kapsam dýþýna (en baþa) alýnarak çakýþma engellendi.

            if (isMovementModeActive && Physics.Raycast(ray, out hit, 500f, gridLayer))
            {
                TileControl tile = hit.collider.GetComponent<TileControl>();
                if (tile != null && ValidatePath(transform.position, tile.transform.position))
                {
                    InitiateMove(tile.transform.position);
                }
            }
            else if (Physics.Raycast(ray, out hit, 500f)) // "out RaycastHit hit" yerine doðrudan tanýmlý "out hit" kullanýldý.
            {
                UnitController targetUnit = hit.collider.GetComponent<UnitController>();
                if (targetUnit != null)
                {
                    FocusOnUnit(targetUnit);
                    return;
                }

                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null && isSelectingTarget && selectedUnit == this)
                {
                    ExecuteCombat(enemy);
                }
            }
        }
    }

    // EKSÝK OLAN METHOT GÖVDESÝ EKLENDÝ
    private void ProcessHoverEffects()
    {
        if (selectedUnit != this) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (isMovementModeActive)
        {
            if (Physics.Raycast(ray, out hit, 500f, gridLayer))
            {
                TileControl tile = hit.collider.GetComponent<TileControl>();
                if (tile != null && tile != lastHoveredTile)
                {
                    if (lastHoveredTile != null) lastHoveredTile.HideRange();

                    bool canMove = ValidatePath(transform.position, tile.transform.position);
                    tile.ShowRange(canMove);
                    lastHoveredTile = tile;
                }
            }
            else if (lastHoveredTile != null)
            {
                lastHoveredTile.HideRange();
                lastHoveredTile = null;
            }
        }
        else if (isSelectingTarget)
        {
            if (Physics.Raycast(ray, out hit, 500f))
            {
                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null && enemy != lastHoveredEnemy)
                {
                    if (lastHoveredEnemy != null) lastHoveredEnemy.ClearRangeText();

                    int dist = Mathf.RoundToInt(Vector3.Distance(transform.position, enemy.transform.position) / stepSize);
                    if (dist <= attackRange)
                    {
                        enemy.SetRangeText($"<color=green>RNG: {dist} (IN RANGE)</color>");
                    }
                    else
                    {
                        enemy.SetRangeText($"<color=red>RNG: {dist} (TOO FAR)</color>");
                    }
                    lastHoveredEnemy = enemy;
                }
            }
            else if (lastHoveredEnemy != null)
            {
                lastHoveredEnemy.ClearRangeText();
                lastHoveredEnemy = null;
            }
        }
    }

    public void FocusOnUnit(UnitController unit)
    {
        UnitController[] units = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (var u in units) u.Deselect();

        selectedUnit = unit;
        if (unit.selectionRing != null) unit.selectionRing.SetActive(true);
        OnUnitSelected?.Invoke(unit);
    }

    public void Deselect()
    {
        isSelectingTarget = false;
        isMovementModeActive = false;
        if (selectionRing != null) selectionRing.SetActive(false);
        ResetHoverVisuals();
    }

    public void RefreshUnit()
    {
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        Deselect();
    }

    private void ResetHoverVisuals()
    {
        if (lastHoveredEnemy != null) { lastHoveredEnemy.ClearRangeText(); lastHoveredEnemy = null; }
        if (lastHoveredTile != null) { lastHoveredTile.HideRange(); lastHoveredTile = null; }
    }

    private void InitiateMove(Vector3 destination)
    {
        float targetX = Mathf.Round(destination.x / stepSize) * stepSize;
        float targetZ = Mathf.Round(destination.z / stepSize) * stepSize;
        Vector3 finalCoords = new Vector3(targetX, transform.position.y, targetZ);

        int cost = Mathf.RoundToInt((Mathf.Abs(finalCoords.x - transform.position.x) + Mathf.Abs(finalCoords.z - transform.position.z)) / stepSize);

        if (cost > 0 && cost <= currentMovementPoints && !IsSpaceOccupiedByUnit(finalCoords))
        {
            UpdateGridStatus(transform.position, false);
            if (lastHoveredTile != null) { lastHoveredTile.HideRange(); lastHoveredTile = null; }

            targetPosition = finalCoords;
            isMoving = true;
            currentMovementPoints -= cost;
            isMovementModeActive = false;

            OnUnitActionPointsChanged?.Invoke(this);
        }
    }

    private bool ValidatePath(Vector3 start, Vector3 end)
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
            TileControl tile = GetTileAt(checkPos);
            if ((tile != null && tile.isOccupied) || IsSpaceOccupiedByUnit(checkPos)) return false;
        }
        TileControl targetTile = GetTileAt(new Vector3(endX * stepSize, start.y, endZ * stepSize));
        return targetTile != null && !targetTile.isOccupied && !IsSpaceOccupiedByUnit(end);
    }

    private IEnumerator DelayedDamage(EnemyController target, int dmg, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            target.TakeDamage(dmg);
            target.SetRangeText("<color=yellow>HIT!</color>");
        }
    }

    private IEnumerator DelayedMiss(EnemyController target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null) target.SetRangeText("<color=red>MISS</color>");
    }

    private IEnumerator ReleaseInteractionLock(EnemyController target, float time)
    {
        yield return new WaitForSeconds(time);
        if (target != null) target.ClearRangeText();
        isInteractionLocked = false;
    }
}