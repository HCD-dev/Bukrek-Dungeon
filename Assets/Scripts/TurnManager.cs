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

        // Event Dinleyicileri (Kayýt)
        UnitBase.OnUnitSpawned += RegisterUnit;
        UnitBase.OnUnitDespawned += UnregisterUnit;
    }

    private void OnDestroy()
    {
        // Hafýza sýzýntýsýný önlemek için Unsubscribe
        UnitBase.OnUnitSpawned -= RegisterUnit;
        UnitBase.OnUnitDespawned -= UnregisterUnit;
    }

    private void Start()
    {
        CurrentPhase = TurnPhase.Player;
    }

    private void RegisterUnit(UnitBase unit)
    {
        if (unit is EnemyController enemy && !activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    private void UnregisterUnit(UnitBase unit)
    {
        if (unit is EnemyController enemy)
        {
            activeEnemies.Remove(enemy);
        }
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

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = activeEnemies[i];
            if (enemy == null) continue;

            enemy.BeginTurn();
            yield return new WaitForSeconds(delayBetweenEnemies);
        }

        CurrentPhase = TurnPhase.Player;
    }
}