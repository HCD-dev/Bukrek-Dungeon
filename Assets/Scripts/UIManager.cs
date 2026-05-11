using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("UI Elemanlarý")]
    public GameObject infoPanel;
    public Image unitPortrait;
    public TextMeshProUGUI movementText;
    public TextMeshProUGUI actionText;
    public TextMeshProUGUI nameText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
       
    }

    public void ShowUnitInfo(UnitController targetUnit)
    {
        if (targetUnit == null) return;

        // Ýsim kýsmýný günceller
        if (nameText != null)
            nameText.text = "<b>" + targetUnit.unitName + "</b>";

        // Aksiyon kýsmýný istediðin formata sokar
        if (actionText != null)
        {
            // Sadece Aksiyon puanýný gösterir
            actionText.text = "Action: " + targetUnit.actionPoints;
        }
    }

    public void HideUI()
    {
        if (infoPanel) infoPanel.SetActive(false);
    }
}