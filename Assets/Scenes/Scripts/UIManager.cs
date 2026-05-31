using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI actionPointsLabel;
    [SerializeField] private TextMeshProUGUI movePointsLabel;
    [SerializeField] private TextMeshProUGUI healthLabel;
    [SerializeField] private TextMeshProUGUI damageLabel;
    [SerializeField] private TextMeshProUGUI rangeLabel;
    [SerializeField] private TextMeshProUGUI hitChanceLabel;
    [SerializeField] private TextMeshProUGUI dodgeLabel;

    [Header("Visual Elements")]
    [SerializeField] private Image portraitDisplay;
    [SerializeField] private Image attackIconSlot;
    [SerializeField] private Image movementIconSlot;

    [Header("Resources")]
    public Sprite erlikPortrait, mergenPortrait;
    public Sprite meleeIcon, rangedIcon;
    public Sprite movementIconSprite;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ResetDisplay();
    }

    public void ShowUnitInfo(UnitController unit)
    {
        if (unit == null) return;

        nameLabel.text = $"<b>{unit.unitName}</b>";
        actionPointsLabel.text = $"AP: {unit.currentActionPoints}";
        movePointsLabel.text = $"MP: {unit.currentMovementPoints}";
        healthLabel.text = $"HP: {unit.currentHealth} / {unit.maxHealth}";
        damageLabel.text = $"DMG: {unit.attackPower}";
        rangeLabel.text = $"RNG: {unit.attackRange}";
        hitChanceLabel.text = $"HIT: %{unit.hitChance}";

        if (dodgeLabel != null)
            dodgeLabel.text = $"DGE: %{unit.dodgeChance}";

        UpdateIcons(unit);
    }

    private void UpdateIcons(UnitController unit)
    {
        portraitDisplay.color = Color.white;
        portraitDisplay.sprite = unit.unitName == "Mergen" ? mergenPortrait : erlikPortrait;

        // Attack Icon Logic
        attackIconSlot.sprite = unit.unitName == "Mergen" ? rangedIcon : meleeIcon;
        attackIconSlot.color = unit.currentActionPoints > 0 ? Color.white : new Color(1, 1, 1, 0.3f);

        // Movement Icon Logic
        if (movementIconSlot != null)
        {
            movementIconSlot.sprite = movementIconSprite;
            movementIconSlot.color = unit.currentMovementPoints > 0 ? Color.white : new Color(1, 1, 1, 0.3f);
        }
    }

    public void ResetDisplay()
    {
        nameLabel.text = string.Empty;
        actionPointsLabel.text = string.Empty;
        movePointsLabel.text = string.Empty;
        healthLabel.text = string.Empty;
        damageLabel.text = string.Empty;
        rangeLabel.text = string.Empty;
        hitChanceLabel.text = string.Empty;

        if (dodgeLabel != null) dodgeLabel.text = string.Empty;

        portraitDisplay.color = Color.clear;
        attackIconSlot.color = Color.clear;
        if (movementIconSlot != null) movementIconSlot.color = Color.clear;
    }

    public void ToggleMovementMode()
    {
        var unit = UnitController.selectedUnit;
        if (unit != null && unit.currentMovementPoints > 0)
        {
            unit.isMovementModeActive = true;
            unit.isSelectingTarget = false;
        }
    }

    public void ToggleAttackMode()
    {
        var unit = UnitController.selectedUnit;
        if (unit != null && unit.currentActionPoints > 0)
        {
            unit.isSelectingTarget = true;
            unit.isMovementModeActive = false;
        }
    }

    public void CommitEndTurn()
    {
        UnitController[] playerUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (var unit in playerUnits)
        {
            unit.RefreshUnit(); // ResetPoints yerine yeni metod adý
            unit.Deselect();    // DeselectUnit yerine yeni metod adý
        }

        UnitController.selectedUnit = null;
        ResetDisplay();

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.FinalizePlayerTurn(); // OnEndTurnButtonPressed yerine yeni metod adý
        }
    }

    public void ReloadLevel()
    {
        UnitController.selectedUnit = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }



}