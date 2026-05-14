using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public enum TurnState { PlayerTurn, EnemyTurn, Busy }
    public TurnState currentState;

    private List<EnemyController> allEnemies = new List<EnemyController>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Oyuna oyuncu turuyla baþla
        currentState = TurnState.PlayerTurn;
        RefreshEnemyList();
    }

    // Bu fonksiyonu UI Butonuna baðlayacaksýn
    public void OnEndTurnButtonPressed()
    {
        if (currentState == TurnState.PlayerTurn)
        {
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        currentState = TurnState.EnemyTurn;
        Debug.Log("Düþman Turu Baþladý!");

        // Sahnedeki tüm düþmanlarý bul
        RefreshEnemyList();

        foreach (EnemyController enemy in allEnemies)
        {
            if (enemy != null)
            {
                // Düþmanýn sýrasýný iþle ve bitmesini bekle
                enemy.ExecuteTurn();
                // Düþman hareket ederken bekleme süresi (Düþman baþýna 2 saniye gibi)
                yield return new WaitForSeconds(2.5f);
            }
        }

        Debug.Log("Oyuncu Turu Baþladý!");
        currentState = TurnState.PlayerTurn;
    }

    void RefreshEnemyList()
    {
        allEnemies.Clear();
        allEnemies.AddRange(Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
    }
}