using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("Düþman Ayarlarý")]
    public string enemyName = "Börü";
    public int maxHealth = 100;
    public int currentHealth; // Mevcut can
    public int damage = 15;
    public int attackRange = 1;
    public int hitChance = 75;
    public int dodgeChance = 10;

    [Header("UI Elemanlarý")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enemyRangeText;

    void Start()
    {
        // ÖNEMLÝ: Baþlangýçta mevcut caný maksimuma eþitle
        currentHealth = maxHealth;

        if (enemyRangeText != null) enemyRangeText.text = "";
        UpdateHPUI();
    }

    public void SetRangeText(string message)
    {
        if (enemyRangeText != null) enemyRangeText.text = message;
    }

    public void ClearRangeText()
    {
        if (enemyRangeText != null) enemyRangeText.text = "";
    }

    public void TakeDamage(int damageAmount)
    {
        // HATA DÜZELTME: Hasarý maxHealth'ten deðil currentHealth'ten düþüyoruz
        currentHealth -= damageAmount;

        // Canýn 0'ýn altýna düþmesini engellemek için (Görsel açýdan iyi olur)
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(enemyName + " " + damageAmount + " hasar aldý! Kalan Can: " + currentHealth);
        UpdateHPUI();

        if (currentHealth <= 0) Die();
    }

    void UpdateHPUI()
    {
        if (hpText != null)
        {
            // \n kullanarak alt satýra geçirebilirsin, daha düzenli durur
            hpText.text = "HP: " + currentHealth + "\nDODGE: %" + dodgeChance;
        }
    }

    void Die()
    {
        Debug.Log(enemyName + " yok edildi!");
        Destroy(gameObject);
    }
}