using UnityEngine;
using TMPro;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Düþman Ayarlarý")]
    public string enemyName = "Börü";
    public int maxHealth = 100;
    public int currentHealth;
    public int damage = 15;
    public int attackRange = 1;
    public int hitChance = 75;
    public int dodgeChance = 10;
    public int moveRange = 2;

    [Header("Hareket Ayarlarý")]
    public float moveSpeed = 8f; // Iþýnlanmak yerine bu hýzda gidecek
    private bool isMoving = false;
    private Vector3 targetMovePosition;

    [Header("UI Elemanlarý")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enemyRangeText;

    private UnitController playerUnit;
    private float stepSize = 10f;

    void Start()
    {
        currentHealth = maxHealth;
        playerUnit = Object.FindAnyObjectByType<UnitController>();
        if (enemyRangeText != null) enemyRangeText.text = "";
        UpdateHPUI();
    }

    void Update()
    {
        // Eðer hareket halindeyse yumuþakça hedef noktaya git
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetMovePosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetMovePosition) < 0.01f)
            {
                transform.position = targetMovePosition;
                isMoving = false;
            }
        }
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
        int currentDist = Mathf.RoundToInt(distance);

        if (currentDist <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            MoveTowardsPlayer();
            // Hareketin bitmesini bekle (isMoving false olana kadar)
            yield return new WaitUntil(() => !isMoving);

            yield return new WaitForSeconds(0.5f);
            distance = Vector3.Distance(transform.position, playerUnit.transform.position) / stepSize;
            if (Mathf.RoundToInt(distance) <= attackRange)
            {
                AttackPlayer();
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        if (playerUnit == null) return;

        Vector3 direction = (playerUnit.transform.position - transform.position).normalized;
        Vector3 rawTargetPos = transform.position + direction * (moveRange * stepSize);

        float targetX = Mathf.Round(rawTargetPos.x / stepSize) * stepSize;
        float targetZ = Mathf.Round(rawTargetPos.z / stepSize) * stepSize;

        targetMovePosition = new Vector3(targetX, transform.position.y, targetZ);
        isMoving = true; // Update içindeki hareketi baþlatýr
        Debug.Log(enemyName + " oyuncuya doðru süzülüyor...");
    }

    private void AttackPlayer()
    {
        if (playerUnit == null) return;

        // Ýsabet kontrolü
        int roll = Random.Range(1, 101);
        int finalHitChance = hitChance - playerUnit.dodgeChance;

        if (roll <= finalHitChance)
        {
            Debug.Log("<color=red>" + enemyName + " vurdu!</color>");
            playerUnit.TakeDamage(damage); // Mergen/Erlik hasar alýr
        }
        else
        {
            Debug.Log("<color=white>" + enemyName + " ISKALADI!</color>");
        }
    }

    // --- SENÝN KODLARIN ---
    public void SetRangeText(string message) { if (enemyRangeText != null) enemyRangeText.text = message; }
    public void ClearRangeText() { if (enemyRangeText != null) enemyRangeText.text = ""; }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHPUI();
        if (currentHealth <= 0) Die();
    }

    void UpdateHPUI() { if (hpText != null) hpText.text = "HP: " + currentHealth + "\nDODGE: %" + dodgeChance; }
    void Die() { Destroy(gameObject); }
}