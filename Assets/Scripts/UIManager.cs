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
    public TextMeshProUGUI dodgeText;

    [Header("Görseller")]
    public Image unitPortrait;
    public Image attackActionImage;
    public Image movementActionImage; // YENÝ: Hareket ikonu için Image bileþeni
    public Sprite erlikPortrait, mergenPortrait;
    public Sprite meleeIcon, rangedIcon;
    public Sprite movementIconSprite; // YENÝ: Hareket ikonu sprite'ý

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

        // YENÝ: Hareket resmi sadece MP varsa tam görünür
        if (movementActionImage != null)
        {
            movementActionImage.sprite = movementIconSprite;
            movementActionImage.color = unit.currentMovementPoints > 0 ? Color.white : new Color(1, 1, 1, 0.3f);
        }

        if (dodgeText != null)
        {
            dodgeText.text = "DODGE: %" + unit.dodgeChance;
        }
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
        if (dodgeText != null)
        {
            dodgeText.text = "";
        }

        // YENÝ: Temizlerken hareket ikonunu da gizle
        if (movementActionImage != null) movementActionImage.color = new Color(0, 0, 0, 0);
    }

    // YENÝ: Hareket ikonuna týklandýðýnda çalýþacak fonksiyon
    public void OnMovementImageClick()
    {
        if (UnitController.selectedUnit != null)
        {
            if (UnitController.selectedUnit.currentMovementPoints > 0)
            {
                UnitController.selectedUnit.isMovementModeActive = true;
                UnitController.selectedUnit.isSelectingTarget = false; // Saldýrý modunu kapat
                Debug.Log("Hareket modu aktif!");
            }
            else
            {
                Debug.Log("Hareket puaný yetersiz!");
            }
        }
    }

    public void OnAttackImageClick()
    {
        if (UnitController.selectedUnit != null)
        {
            if (UnitController.selectedUnit.currentActionPoints > 0)
            {
                UnitController.selectedUnit.isSelectingTarget = true;
                UnitController.selectedUnit.isMovementModeActive = false; // Hareket modunu kapat
                Debug.Log("Saldýrý modu aktif!");
            }
            else
            {
                Debug.Log("Saldýrý puaný yetersiz!");
            }
        }
    }

    public void EndTurn()
    {
        // 1. Oyuncu birimlerini sýfýrla
        UnitController[] allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (UnitController unit in allUnits)
        {
            unit.ResetPoints();
            unit.DeselectUnit();
        }

        UnitController.selectedUnit = null;
        ClearUI();

        // 2. TURN MANAGER'A HABER VER (Eklendi)
        // Eðer sahnede TurnManager varsa düþman turunu baþlatýr
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnEndTurnButtonPressed();
        }

        Debug.Log("<color=cyan>Tur Sonlandýrýldý! Düþman sýrasý kontrol ediliyor.</color>");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        UnitController.selectedUnit = null;
        Debug.Log("<color=yellow>Oyun Sýfýrlandý!</color>");
    }
}