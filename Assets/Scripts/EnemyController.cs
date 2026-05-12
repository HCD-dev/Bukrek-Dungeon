using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("Düþman Ayarlarý")]
    public string enemyName = "Karakoncolos";
    public int health = 100;
    public TextMeshProUGUI hpText; // Baþýnýn üzerindeki TMP

    void Start()
    {
        UpdateHPUI();
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log(enemyName + " " + damage + " hasar aldý!");
        UpdateHPUI();

        if (health <= 0) Die();
    }

    void UpdateHPUI()
    {
        if (hpText != null) hpText.text = "HP: " + health;
    }

    void Die()
    {
        Debug.Log(enemyName + " yok edildi!");
        Destroy(gameObject);
    }
}