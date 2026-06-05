using UnityEngine;
using System;

public interface IDamageable
{
    void TakeDamage(int amount);
    int CurrentHealth { get; }
}

public abstract class UnitBase : MonoBehaviour, IDamageable
{
    [Header("Identity & Base Stats")]
    public string unitName;
    public int maxHealth = 100;
    [SerializeField] protected int currentHealth;
    public int attackPower = 20;
    public int attackRange = 1;
    public int hitChance = 85;
    public int dodgeChance = 0;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float stepSize = 10f;
    public LayerMask gridLayer;

    protected bool isMoving = false;
    protected Vector3 targetPosition;
    protected Animator animator;

    // Kapsülleme (Encapsulation)
    public int CurrentHealth => currentHealth;

    // Global Eventler (Observer Pattern)
    public static event Action<UnitBase> OnUnitSpawned;
    public static event Action<UnitBase> OnUnitDespawned;
    public event Action<int, int> OnHealthChanged; // (current, max)

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        targetPosition = transform.position;
        UpdateGridStatus(transform.position, true);
        OnUnitSpawned?.Invoke(this);
    }

    protected virtual void Update()
    {
        HandleMovementAnimation();
    }

    protected virtual void HandleMovementAnimation()
    {
        if (animator != null && HasParameter("isWalking"))
        {
            animator.SetBool("isWalking", isMoving);
        }
    }

    public virtual void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (animator != null && currentHealth > 0 && HasParameter("Damage"))
        {
            animator.SetTrigger("Damage");
        }

        if (currentHealth <= 0)
        {
            ProcessDeath();
        }
    }

    protected virtual void ProcessDeath()
    {
        UpdateGridStatus(transform.position, false);
        OnUnitDespawned?.Invoke(this);

        if (animator != null && HasParameter("Death"))
        {
            animator.SetTrigger("Death");
            Destroy(gameObject, 2.0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Ortak Grid Yardýmcý Metotlarý
    protected void UpdateGridStatus(Vector3 pos, bool occupied)
    {
        TileControl tile = GetTileAt(pos);
        if (tile != null) tile.isOccupied = occupied;
    }

    protected TileControl GetTileAt(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, gridLayer))
        {
            return hit.collider.GetComponent<TileControl>();
        }
        return null;
    }

    protected bool IsSpaceOccupiedByUnit(Vector3 pos)
    {
        float radius = stepSize * 0.4f;
        Collider[] hits = Physics.OverlapSphere(new Vector3(pos.x, pos.y + 0.5f, pos.z), radius);
        foreach (var col in hits)
        {
            if (col.GetComponent<UnitBase>() != null && col.gameObject != gameObject)
                return true;
        }
        return false;
    }

    protected bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}