using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("UI Elemanlarý")]
    public GameObject infoPanel;
    public TextMeshProUGUI statsText;
    public Image unitPortrait; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (infoPanel) infoPanel.SetActive(false);
    }

    public void ShowUnitInfo(UnitController unit)
    {
        if (infoPanel == null) return;

        infoPanel.SetActive(true);
        statsText.text = $"<b>{unit.unitName}</b>\n" +
                         $"Hareket: {unit.currentMovementPoints} / {unit.maxMovementPoints}\n" +
                         $"Aksiyon: {unit.actionPoints}";
    }

    public void HideUI()
    {
        if (infoPanel) infoPanel.SetActive(false);
    }
}