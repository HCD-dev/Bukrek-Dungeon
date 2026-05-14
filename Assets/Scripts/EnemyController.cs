using UnityEngine;
using TMPro;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Düþman Ayarlarý")]
    public string enemyName = "Karakoncolos";
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 15;
    public int attackRange = 1;
    public int hitChance = 75;
    public int dodgeChance = 10;
    public int moveRange = 2;

    [Header("Hareket Ayarlarý")]
    public float moveSpeed = 8f;
    private bool isMoving = false;
    private Vector3 targetMovePosition;
    private Vector3 positionBeforeMove;

    [Header("UI Elemanlarý")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enemyRangeText;

    private UnitController playerUnit;
    public float stepSize = 10f;

    [Header("Sistem Ayarlarý")]
    public LayerMask gridLayer;
                
    void Start()
    {
        currentHealth = maxHealth;
        playerUnit = Object.FindAnyObjectByType<UnitController>();
        if (enemyRangeText != null) enemyRangeText.text = "";
        UpdateHPUI();

        SetTileOccupied(transform.position, true);
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetMovePosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetMovePosition) < 0.01f)
            {
                transform.position = targetMovePosition;
                isMoving = false;

                SetTileOccupied(positionBeforeMove, false);
                SetTileOccupied(transform.position, true);
            }
        }
    }

    private void SetTileOccupied(Vector3 pos, bool occupied)
    {
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 10f, gridLayer))
        {
            TileControl tile = hit.collider.GetComponent<TileControl>();
            if (tile != null) tile.isOccupied = occupied;
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

    private bool IsTileOccupiedByUnit(Vector3 pos)
    {
        float checkRadius = stepSize * 0.4f;
        Vector3 center = new Vector3(pos.x, pos.y + 0.5f, pos.z);
        Collider[] cols = Physics.OverlapSphere(center, checkRadius);
        foreach (var c in cols)
        {
            if (c.GetComponent<UnitController>() != null || c.GetComponent<EnemyController>() != null)
                return true;
        }
        return false;
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
            if (currentX != endGridX) currentX += stepX;
            if (currentZ != endGridZ) currentZ += stepZ;

            // Hedef kareyi atla; hedef ayrýca kontrol edilecek
            if (currentX == endGridX && currentZ == endGridZ) break;

            Vector3 checkPos = new Vector3(currentX * stepSize, start.y, currentZ * stepSize);
            TileControl tile = GetTileAtPosition(checkPos);
            if (tile != null && tile.isOccupied) return false;
            if (IsTileOccupiedByUnit(checkPos)) return false;
        }

        TileControl targetTile = GetTileAtPosition(gridEnd);
        if (targetTile != null && targetTile.isOccupied) return false;
        if (IsTileOccupiedByUnit(gridEnd)) return false;

        return true;
    }

    public void ExecuteTurn()
    {
        StartCoroutine(AIActionRoutine());
    }

    private IEnumerator AIActionRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (playerUnit == null) yield break;

        float distance = Vector3.Distance(transform.position, playerUnit.transform.position) / stepSize;
        if (Mathf.RoundToInt(distance) <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            MoveTowardsPlayer();
            yield return new WaitUntil(() => !isMoving);

            yield return new WaitForSeconds(0.5f);
            distance = Vector3.Distance(transform.position, playerUnit.transform.position) / stepSize;
            if (Mathf.RoundToInt(distance) <= attackRange) AttackPlayer();
        }
    }

    private void MoveTowardsPlayer()
    {
        if (playerUnit == null) return;

        Vector3 direction = (playerUnit.transform.position - transform.position).normalized;
        Vector3 rawTargetPos = transform.position + direction * (moveRange * stepSize);

        float targetX = Mathf.Round(rawTargetPos.x / stepSize) * stepSize;
        float targetZ = Mathf.Round(rawTargetPos.z / stepSize) * stepSize;
        Vector3 desiredTarget = new Vector3(targetX, transform.position.y, targetZ);

        // Eðer doðrudan hedef doluysa ya da hedefe yol kapalýysa alternatif yakýn kare ara
        if (!IsTileOccupiedByUnit(desiredTarget) && IsPathClear(transform.position, desiredTarget))
        {
            positionBeforeMove = transform.position;
            targetMovePosition = desiredTarget;
            isMoving = true;
            return;
        }

        // Alternatif arama: moveRange içinde en yakýn uygun kareyi bul
        Vector3 bestCandidate = transform.position;
        float bestDistToPlayer = float.MaxValue;
        int range = Mathf.Max(1, moveRange);

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dz = -range; dz <= range; dz++)
            {
                int chebyshev = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
                if (chebyshev == 0 || chebyshev > range) continue;

                Vector3 cand = new Vector3(
                    Mathf.Round((transform.position.x + dx * stepSize) / stepSize) * stepSize,
                    transform.position.y,
                    Mathf.Round((transform.position.z + dz * stepSize) / stepSize) * stepSize
                );

                if (IsTileOccupiedByUnit(cand)) continue;
                if (!IsPathClear(transform.position, cand)) continue;

                float distToPlayer = Vector3.Distance(cand, playerUnit.transform.position);
                if (distToPlayer < bestDistToPlayer)
                {
                    bestDistToPlayer = distToPlayer;
                    bestCandidate = cand;
                }
            }
        }

        if (bestDistToPlayer < float.MaxValue && bestCandidate != transform.position)
        {
            positionBeforeMove = transform.position;
            targetMovePosition = bestCandidate;
            isMoving = true;
        }
        else
        {
            // Hiç uygun kare yok
            //Debug.Log(enemyName + " için uygun hareket bulunamadý.");
        }
    }

    private void AttackPlayer()
    {
        if (playerUnit == null) return;
        int roll = Random.Range(1, 101);
        if (roll <= (hitChance - playerUnit.dodgeChance))
        {
            playerUnit.TakeDamage(damage);
        }
    }

    public void SetRangeText(string message) { if (enemyRangeText != null) enemyRangeText.text = message; }
    public void ClearRangeText() { if (enemyRangeText != null) enemyRangeText.text = ""; }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHPUI();
        if (currentHealth <= 0) Die();
    }

    void UpdateHPUI()
    {
        if (hpText != null) hpText.text = "HP: " + currentHealth + "\nDODGE: %" + dodgeChance;
    }

    void Die()
    {
        SetTileOccupied(transform.position, false);
        Destroy(gameObject);
    }
}