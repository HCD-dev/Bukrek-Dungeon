using UnityEngine;
using TMPro;
using KevinIglesias; // Sadece Mergen'in okçu paketi için kalýyor
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class UnitController : MonoBehaviour
{
    private bool isInteractionLocked = false;
    [HideInInspector] public bool isMovementModeActive = false;
    private EnemyController lastHoveredEnemy;
    public static UnitController selectedUnit;

    // --- EVENT'LER ---
    public static event Action<UnitController> OnMovementModeStarted;
    public static event Action<UnitController, Vector3> OnMovementCompleted;

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

    // --- MERGEN ÝÇÝN OKÇU SCRIPT REFERANSI ---
    private HumanArcherController archerCtrl;

    void Start()
    {
        // Karakterin kendisindeki veya alt modelindeki Animator'ý bulur
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // --- MERGEN (OKÇU) BAÞLANGIÇ AYARI ---
        if (unitName == "Mergen")
        {
            archerCtrl = GetComponentInChildren<HumanArcherController>(true);
            if (archerCtrl != null)
            {
                archerCtrl.gameObject.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
        }

        currentHealth = maxHealth;
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        targetPosition = transform.position;

        if (selectionRing != null) selectionRing.SetActive(false);
        SetTileStatus(transform.position, true);
    }

    public void ActivateMovementMode()
    {
        if (isInteractionLocked || isMoving || currentMovementPoints <= 0) return;

        isMovementModeActive = true;
        isSelectingTarget = false;

        OnMovementModeStarted?.Invoke(this);
    }

    void Update()
    {
        if (TurnManager.Instance == null) return;
        if (TurnManager.Instance.CurrentPhase != TurnManager.TurnPhase.Player) return;

        // --- WALK / YÜRÜME ANÝMASYON KONTROLÜ ---
        if (animator != null)
        {
            // Animator pencerendeki 'isWalking' parametresini günceller
            if (HasParameter("isWalking"))
            {
                animator.SetBool("isWalking", isMoving);
            }
        }

        HandleInteraction();
        ProcessHoverEffects();

        if (isMoving)
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
                SetTileStatus(transform.position, true);

                OnMovementCompleted?.Invoke(this, transform.position);
            }
        }
    }

    // --- ATTACK / SALDIRI ANÝMASYON KONTROLÜ ---
    public void ExecuteCombat(EnemyController target)
    {
        if (currentActionPoints <= 0 || isInteractionLocked) return;

        int dist = Mathf.RoundToInt(Vector3.Distance(transform.position, target.transform.position) / stepSize);
        if (dist <= attackRange)
        {
            isInteractionLocked = true;
            currentActionPoints--;

            Vector3 targetDirection = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.LookAt(targetDirection);

            // 1) MERGEN (Kevin Iglesias Okçu Sistemi)
            if (unitName == "Mergen" && archerCtrl != null)
            {
                archerCtrl.PlayArcherAnimation(ArcherAnimation.ShootRunning);
                archerCtrl.LoadBow(0f, 0.4f);
                archerCtrl.ShootArrow(0.4f, 0.05f);
                archerCtrl.GetArrow(0.9f);
            }
            // 2) ERLÝK veya Diðer Durumlar (Doðrudan Animator Trigger'larý)
            else
            {
                if (animator != null && HasParameter("Attack"))
                {
                    animator.SetTrigger("Attack");
                }
            }

            // Hasar verme animasyon zamanlamasý (0.6 saniye sonra vurur)
            if (UnityEngine.Random.Range(1, 101) <= (hitChance - target.dodgeChance))
            {
                StartCoroutine(DelayedDamage(target, attackPower, 0.6f));
            }
            else
            {
                StartCoroutine(DelayedMiss(target, 0.6f));
            }

            StartCoroutine(ReleaseInteractionLock(target, 1.5f));
            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        }
    }

    // --- DAMAGE / HASAR ALMA ANÝMASYON KONTROLÜ ---
    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        // Karakter ölmediyse hasar alma ("Damage" Trigger) animasyonunu oynatýr
        if (animator != null && currentHealth > 0)
        {
            if (HasParameter("Damage"))
            {
                animator.SetTrigger("Damage");
            }
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        if (currentHealth <= 0) HandleDeath();
    }

    // --- DEATH / ÖLÜM ANÝMASYON KONTROLÜ ---
    void HandleDeath()
    {
        SetTileStatus(transform.position, false);
        isInteractionLocked = true;

        if (animator != null && HasParameter("Death"))
        {
            // Ölüm ("Death" Trigger) animasyonunu baþlatýr
            animator.SetTrigger("Death");

            // Animasyon klibinin tamamlanmasý için karakteri 2.0 saniye sonra sahnenden siler
            Destroy(gameObject, 2.0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- PARAMETRE GÜVENLÝK KONTROLÜ ---
    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
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
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, gridLayer)) // Sadece grid katmanýna bakýyoruz
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

                    // --- HATA DÜZELTMESÝ: GIZLÝ ÇAPRAZ HESAPLAMAYI ENGELLEME (MANHATTAN DISTANCE) ---
                    float targetX = Mathf.Round(tile.transform.position.x / stepSize) * stepSize;
                    float targetZ = Mathf.Round(tile.transform.position.z / stepSize) * stepSize;

                    int gridDistX = Mathf.RoundToInt(Mathf.Abs(targetX - transform.position.x) / stepSize);
                    int gridDistZ = Mathf.RoundToInt(Mathf.Abs(targetZ - transform.position.z) / stepSize);
                    int totalGridCost = gridDistX + gridDistZ; // Karakterin harcayacaðý gerçek hareket puaný

                    // Hem puaný yetmeli hem de önünde engel olmamalý
                    bool isValid = totalGridCost <= currentMovementPoints && totalGridCost > 0 && ValidatePath(transform.position, tile.transform.position);

                    tile.ShowRange(isValid);
                    return;
                }
            }
        }
        ResetHoverVisuals();
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

            // --- HATA DÜZELTMESÝ: HAREKET BAÞLADIÐI AN ESKÝ GÖRSELÝ SIFIRLA ---
            if (lastHoveredTile != null)
            {
                lastHoveredTile.HideRange();
                lastHoveredTile = null;
            }

            targetPosition = finalCoords;
            isMoving = true;
            currentMovementPoints -= cost;

            // Hareket modunu kapatýyoruz ki vardýðý yerde karolar otomatik kýrmýzý/yeþil takýlý kalmasýn
            isMovementModeActive = false;

            if (UIManager.Instance != null) UIManager.Instance.ShowUnitInfo(this);
        }
    }

    private void ResetHoverVisuals()
    {
        if (lastHoveredEnemy != null) { lastHoveredEnemy.ClearRangeText(); lastHoveredEnemy = null; }
        if (lastHoveredTile != null) { lastHoveredTile.HideRange(); lastHoveredTile = null; }
    }

    void HandleInteraction()
    {
        // Eðer UI (Arayüz) elementlerine (buton vb.) týklanýyorsa haritaya týklamayý engelle
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // --- DÜZELTME 1: HAREKET MODUNDAYSA SADECE GRID KATMANINA IÞIN AT ---
            if (isMovementModeActive)
            {
                // 500f mesafe içinde sadece gridLayer katmanýndaki objeleri yakala
                if (Physics.Raycast(ray, out hit, 500f, gridLayer))
                {
                    TileControl tile = hit.collider.GetComponent<TileControl>();
                    if (tile != null)
                    {
                        if (ValidatePath(transform.position, tile.transform.position))
                        {
                            InitiateMove(tile.transform.position);
                        }
                    }
                }
            }
            // --- DÜZELTME 2: DÝÐER ETKÝLEÞÝMLER (SEÇÝM VE SALDIRI) ---
            else
            {
                if (Physics.Raycast(ray, out hit, 500f))
                {
                    if (selectedUnit != this)
                    {
                        UnitController targetUnit = hit.collider.GetComponent<UnitController>();
                        if (targetUnit != null) FocusOnUnit(targetUnit);
                        return;
                    }

                    EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                    if (enemy != null && isSelectingTarget)
                    {
                        ExecuteCombat(enemy);
                    }
                }
            }
        }
    }

   

    public void FocusOnUnit(UnitController unit)
    {
        UnitController[] units = UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
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

    public void RefreshUnit()
    {
        currentMovementPoints = maxMovementPoints;
        currentActionPoints = maxActionPoints;
        isSelectingTarget = false;
        isMovementModeActive = false;
        isMoving = false;
    }
}