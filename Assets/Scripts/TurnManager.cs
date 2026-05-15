using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum TurnPhase { Player, Enemy, Processing }
    public TurnPhase CurrentPhase { get; private set; }

    [SerializeField] private float delayBetweenEnemies = 1.5f;
    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CurrentPhase = TurnPhase.Player;
        InitializeEnemies();
    }

    public void FinalizePlayerTurn()
    {
        if (CurrentPhase == TurnPhase.Player)
        {
            StartCoroutine(ProcessEnemyActions());
        }
    }

    private IEnumerator ProcessEnemyActions()
    {
        CurrentPhase = TurnPhase.Enemy;

        // Perform logic for each enemy sequentially
        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            enemy.ExecuteTurn();
            yield return new WaitForSeconds(delayBetweenEnemies);
        }

        ResetToPlayerTurn();
    }

    private void ResetToPlayerTurn()
    {
        CurrentPhase = TurnPhase.Player;
    }

    private void InitializeEnemies()
    {
        activeEnemies.Clear();
        activeEnemies.AddRange(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
    }

    public void RegisterEnemy(EnemyController enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);
    }
}