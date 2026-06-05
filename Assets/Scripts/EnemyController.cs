using UnityEngine;
using TMPro;
using System.Collections;

public class EnemyController : UnitBase
{
    [Header("AI Movement")]
    public int moveRange = 2;
    private Vector3 destinationPoint;
    private Vector3 lastPosition;

    [Header("Interface")]
    public TextMeshProUGUI hpDisplay;
    public TextMeshProUGUI feedbackText;

    private UnitController playerRef;

    protected override void Start()
    {
        base.Start();
        playerRef = FindAnyObjectByType<UnitController>();
        if (feedbackText != null) feedbackText.text = string.Empty;

        RefreshStatusUI();
        OnHealthChanged += (current, max) => RefreshStatusUI(); // Kendi can barýný event ile günceller
    }

    protected override void Update()
    {
        // AI yürüme mantýðý
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, destinationPoint, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, destinationPoint) < 0.01f)
            {
                transform.position = destinationPoint;
                isMoving = false;

                UpdateGridStatus(lastPosition, false);
                UpdateGridStatus(transform.position, true);
            }
        }
    }

    public void BeginTurn()
    {
        StartCoroutine(ProcessAIBehavior());
    }

    private IEnumerator ProcessAIBehavior()
    {
        yield return new WaitForSeconds(0.6f);
        if (playerRef == null) yield break;

        float dist = Vector3.Distance(transform.position, playerRef.transform.position) / stepSize;

        if (Mathf.RoundToInt(dist) <= attackRange)
        {
            PerformStrike();
        }
        else
        {
            CalculateMovement();
            yield return new WaitUntil(() => !isMoving);

            yield return new WaitForSeconds(0.4f);
            dist = Vector3.Distance(transform.position, playerRef.transform.position) / stepSize;
            if (Mathf.RoundToInt(dist) <= attackRange) PerformStrike();
        }
    }

    // ... (CalculateMovement, FindAlternativeSpot, ApplyMove, PerformStrike metodlarý ata sýnýfýn IsSpaceOccupiedByUnit ve GetTileAt yapýlarýný kullanarak korundu)

    private void CalculateMovement()
    {
        if (playerRef == null) return;

        Vector3 direction = (playerRef.transform.position - transform.position).normalized;
        Vector3 targetCoord = transform.position + direction * (moveRange * stepSize);

        float snapX = Mathf.Round(targetCoord.x / stepSize) * stepSize;
        float snapZ = Mathf.Round(targetCoord.z / stepSize) * stepSize;
        Vector3 idealSpot = new Vector3(snapX, transform.position.y, snapZ);

        if (IsNavigationPossible(transform.position, idealSpot))
        {
            ApplyMove(idealSpot);
            return;
        }

        FindAlternativeSpot();
    }

    private void FindAlternativeSpot()
    {
        Vector3 bestSpot = transform.position;
        float minDistance = float.MaxValue;
        int searchRange = Mathf.Max(1, moveRange);

        for (int x = -searchRange; x <= searchRange; x++)
        {
            for (int z = -searchRange; z <= searchRange; z++)
            {
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) > searchRange) continue;

                Vector3 candidate = new Vector3(
                    Mathf.Round((transform.position.x + x * stepSize) / stepSize) * stepSize,
                    transform.position.y,
                    Mathf.Round((transform.position.z + z * stepSize) / stepSize) * stepSize
                );

                if (candidate == transform.position) continue;
                if (!IsNavigationPossible(transform.position, candidate)) continue;

                float distToTarget = Vector3.Distance(candidate, playerRef.transform.position);
                if (distToTarget < minDistance)
                {
                    minDistance = distToTarget;
                    bestSpot = candidate;
                }
            }
        }

        if (bestSpot != transform.position) ApplyMove(bestSpot);
    }

    private bool IsNavigationPossible(Vector3 start, Vector3 end)
    {
        int startX = Mathf.RoundToInt(start.x / stepSize);
        int startZ = Mathf.RoundToInt(start.z / stepSize);
        int endX = Mathf.RoundToInt(end.x / stepSize);
        int endZ = Mathf.RoundToInt(end.z / stepSize);

        int stepX = (endX == startX) ? 0 : (endX > startX ? 1 : -1);
        int stepZ = (endZ == startZ) ? 0 : (endZ > startZ ? 1 : -1);

        int currX = startX;
        int currZ = startZ;

        while (currX != endX || currZ != endZ)
        {
            if (currX != endX) currX += stepX;
            if (currZ != endZ) currZ += stepZ;
            if (currX == endX && currZ == endZ) break;

            Vector3 checkPos = new Vector3(currX * stepSize, start.y, currZ * stepSize);
            TileControl tile = GetTileAt(checkPos);
            if ((tile != null && tile.isOccupied) || IsSpaceOccupiedByUnit(checkPos)) return false;
        }

        TileControl targetTile = GetTileAt(end);
        return targetTile != null && !targetTile.isOccupied && !IsSpaceOccupiedByUnit(end);
    }

    private void ApplyMove(Vector3 spot)
    {
        lastPosition = transform.position;
        destinationPoint = spot;
        isMoving = true;
    }

    private void PerformStrike()
    {
        if (playerRef == null) return;
        int chance = hitChance - playerRef.dodgeChance;
        if (Random.Range(1, 101) <= chance)
        {
            playerRef.TakeDamage(attackPower); // damage yerine abstract sýnýftaki attackPower kullanýldý
        }
    }

    public void SetRangeText(string msg) { if (feedbackText != null) feedbackText.text = msg; }
    public void ClearRangeText() { if (feedbackText != null) feedbackText.text = string.Empty; }

    private void RefreshStatusUI()
    {
        if (hpDisplay != null)
            hpDisplay.text = $"HP: {currentHealth}\nDODGE: %{dodgeChance}";
    }
}