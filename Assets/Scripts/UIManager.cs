using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Metinleri")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI actionText;   // Panelde "AP: 1 / 1" yazar
    public TextMeshProUGUI movementText; // Panelde "MP: 3 / 3" yazar
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI hitChanceText;

    [Header("Görseller")]
    public Image unitPortrait;
    public Image attackActionImage;
    public Sprite erlikPortrait, mergenPortrait;
    public Sprite meleeIcon, rangedIcon;

    void Awake() { Instance = this; }
    void Start()
    {
        // Instance kontrolü (Tekil nesne yapýsý için)
        if (Instance == null) Instance = this;

        // Oyun baþlar baþlamaz tüm metinleri ve resimleri temizle
        ClearUI();
    }
    public void ShowUnitInfo(UnitController unit)
    {
        nameText.text = "<b>" + unit.unitName + "</b>";

        // AP ve MP'yi panelde ayrý ayrý gösteriyoruz
        actionText.text = "AP: " + unit.currentActionPoints;
        movementText.text = "MP: " + unit.currentMovementPoints;

        healthText.text = "HP: " + unit.currentHealth + " / " + unit.maxHealth;
        attackText.text = "Damage: " + unit.attackPower;
        rangeText.text = "Range: " + unit.attackRange;
        hitChanceText.text = "Hit Chance: %" + unit.hitChance;

        unitPortrait.color = Color.white;
        // Saldýrý resmi sadece AP varsa tam görünür
        attackActionImage.color = unit.currentActionPoints > 0 ? Color.white : new Color(1, 1, 1, 0.3f);

        if (unit.unitName == "Mergen")
        {
            unitPortrait.sprite = mergenPortrait;
            attackActionImage.sprite = rangedIcon;
        }
        else
        {
            unitPortrait.sprite = erlikPortrait;
            attackActionImage.sprite = meleeIcon;
        }
    }

    public void ClearUI()
    {
        rangeText.text = "";
        hitChanceText.text = "";
        nameText.text = "";
        actionText.text = "";
        movementText.text = "";
        healthText.text = "";
        attackText.text = ""; 
        unitPortrait.color = new Color(0, 0, 0, 0);
        attackActionImage.color = new Color(0, 0, 0, 0);
    }

    public void OnAttackImageClick()
    {
        if (UnitController.selectedUnit != null)
        {
            // Eðer AP (Saldýrý Hakký) 0'dan büyükse seçimi baþlat
            if (UnitController.selectedUnit.currentActionPoints > 0)
            {
                UnitController.selectedUnit.StartTargetSelection();
                Debug.Log(UnitController.selectedUnit.unitName + " için hedef seçiliyor...");
            }
            else
            {
                Debug.Log("<color=red>Saldýrý hakkýn bitti!</color>");
            }
        }
    }

    public void EndTurn()
    {
        // Sahnedeki tüm UnitController'larý bul ve hepsini resetle
        UnitController[] allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (UnitController unit in allUnits)
        {
            unit.ResetPoints();
            unit.DeselectUnit();
        }

        // Seçili üniteyi temizle ve UI'ý kapat
        UnitController.selectedUnit = null;
        ClearUI();

        Debug.Log("<color=cyan>Yeni Tur Baþladý! Tüm puanlar yenilendi.</color>");
    }
    public void RestartGame() // 2. BU FONKSÝYONU EKLE
    {
        // Þu an açýk olan sahnenin adýný alýr ve onu tekrar yükler
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // Statik deðiþkenleri manuel temizle (Önemli!)
        UnitController.selectedUnit = null;

        Debug.Log("<color=yellow>Oyun Sýfýrlandý!</color>");
    }
}