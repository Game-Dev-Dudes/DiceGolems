using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum CombatState {
    Start,
    PlayerPreTurn,
    PlayerMidTurn,
    PlayerPostTurn,
    EnemyPreTurn,
    EnemyMidTurn,
    EnemyPostTurn,
    Win,
    Lose,
    None
};

public class StateCombatController : MonoBehaviour {
    public CombatState State;
    public bool IsDead;
    public bool IsVictorious;
    public bool IsPlayerTurnEnded;

    public PlayerController _PlayerController;
    public EnemyPaul _EnemyController;
    private CombatController _CombatController;
    private ZoneCombatController _ZonesController;
    private UICombatController _UIController;
    private bool _IsStateReady;
    private Dictionary<CombatState, CombatState> _StateMap = new Dictionary<CombatState, CombatState> {
        { CombatState.Start, CombatState.PlayerPreTurn },
        { CombatState.PlayerPreTurn, CombatState.PlayerMidTurn },
        { CombatState.PlayerMidTurn, CombatState.PlayerPostTurn },
        { CombatState.PlayerPostTurn, CombatState.EnemyPreTurn },
        { CombatState.EnemyPreTurn, CombatState.EnemyMidTurn },
        { CombatState.EnemyMidTurn, CombatState.EnemyPostTurn },
        { CombatState.EnemyPostTurn, CombatState.PlayerPreTurn }
    };

    private void Awake() {
        _PlayerController = new PlayerController();
        _EnemyController = new EnemyPaul();
    }
    private void Start() {
        State = CombatState.Start;
        _CombatController = GetComponent<CombatController>();
        _ZonesController = GetComponent<ZoneCombatController>();
        _UIController = GetComponent<UICombatController>();

        _IsStateReady = true;
    }

    private CombatState NextCombatState() {
        if (IsVictorious) {
            return CombatState.Win;
        }

        if (IsDead) {
            return CombatState.Lose;
        }

        return _StateMap[State];
    }

    private void HandleStartState() {
        _IsStateReady = false;

        // [X] set player starting energy
        // [X] copy player bank
        // [X] load first enemy action

        // Copy the bank.
        _CombatController.CopyBank(_PlayerController.Bank);
        _UIController.GetBankInfo();

        // Set the energy level.
        _CombatController.SetEnergy(4);
        _UIController.GetEnergy();

        // Set play and energy level.
        _UIController.UpdatePlayerHealth();
        _UIController.UpdateEnemyHealth();

        // Select enemy first action.
        _EnemyController.DecideAction();

        // if (_EnemyAttacking) {
        //     _UIController.UpdateEnemyAction("Enemy is going to heavy attack!", Color.red);
        // } else {
        //     _UIController.UpdateEnemyAction("Enemy is going to weak attack!", Color.yellow);
        // }

        _IsStateReady = true;
    }

    private void HandlePlayerPreTurnState() {
        _IsStateReady = false;

        // [X] add per-turn energy
        // [ ] refresh tile charges
        // [ ] trigger damage over time
        // [ ] trigger enabled debuffs

        //loop through status effects
        //if(typeof(status) == preTurnStatus){ do it }

        // Update the energy.
        _CombatController.UpdateEnergy();
        _UIController.GetEnergy();

        _PlayerController.HandleStatusEffect(State);
        _EnemyController.HandleStatusEffect(State);

        _IsStateReady = true;
    }

    private IEnumerator HandlePlayerMidTurnState() {
        _IsStateReady = false;

        // [X] driven by UI
        // [ ] lock input controlls

        // Wait for UI input.
        yield return new WaitUntil(() => IsPlayerTurnEnded);
        IsPlayerTurnEnded = false;

        if (_EnemyController.Health < 0) {
            IsVictorious = true;
        }

        _IsStateReady = true;
    }

    private void HandlePlayerPostTurnState() {
        _IsStateReady = false;

        // [X] clear backend dice zones
        // [ ] countdown/clear status effects

        // Clear the dice zones.
        _PlayerController.HandleStatusEffect(State);
        _EnemyController.HandleStatusEffect(State);

        _ZonesController.Clear();

        _IsStateReady = true;
    }

    private void HandleEnemyPreTurnState() {
        _IsStateReady = false;

        _PlayerController.HandleStatusEffect(State);
        _EnemyController.HandleStatusEffect(State);

        // [ ] trigger damage over time
        // [ ] trigger enabled debuffs

        _IsStateReady = true;
    }

    private void HandleEnemyMidTurnState() {
        _IsStateReady = false;

        // [X] execute selected action
        _EnemyController.ExecuteQueuedAction(_PlayerController, State);


        // Execute enemy selected action.
        // int hp;
        // if (_EnemyAttacking) {
        //     hp = _EnemyController.Attack();
        //     _PlayerController.TakeDamage(hp);
        // } else {
        //     hp = _EnemyController.Heal();
        // }

        if (_PlayerController.Health < 0) {
            IsDead = true;
        }

        _UIController.UpdateEnemyHealth();
        _UIController.UpdatePlayerHealth();

        _IsStateReady = true;
    }

    private void HandleEnemyPostTurnState() {
        _IsStateReady = false;

        _PlayerController.HandleStatusEffect(State);
        _EnemyController.HandleStatusEffect(State);

        // [ ] countdown/clear status effects
        // [X] select enemy action (attack/defend)

        _EnemyController.DecideAction();

        //send queued action type to ui
        //UICombatController.setActionType(queuedAction);

        // Setect enemy action.
        //_EnemyAttacking = _EnemyController.GetAction();

        // if (_EnemyAttacking) {
        //     _UIController.UpdateEnemyAction("Enemy is going to attack!", Color.red);
        // } else {
        //     _UIController.UpdateEnemyAction("Enemy is going to heal!", Color.green);
        // }

        _IsStateReady = true;
    }

    private void HandleWinState() {
        _IsStateReady = false;

        // [ ] end combat
        // [ ] clear temporary negative effects
        // [ ] give reward
        // [ ] level up
        // [ ] exit scene

        _UIController.UpdateWinLose("Victory", Color.green);
    }

    private void HandleLoseState() {
        _IsStateReady = false;

        // [ ] end combat
        // [ ] clear temporary negative effects
        // [ ] decrease lives

        _UIController.UpdateWinLose("Defeat", Color.red);
    }

    private void Update() {
        if (!_IsStateReady) {
            return;
        }

        switch (State) {
            case CombatState.Start:
                HandleStartState();
                State = NextCombatState();

                break;

            case CombatState.PlayerPreTurn:
                HandlePlayerPreTurnState();
                State = NextCombatState();

                break;

            case CombatState.PlayerMidTurn:
                StartCoroutine(HandlePlayerMidTurnState());
                State = NextCombatState();

                break;

            case CombatState.PlayerPostTurn:
                HandlePlayerPostTurnState();
                State = NextCombatState();

                break;

            case CombatState.EnemyPreTurn:
                HandleEnemyPreTurnState();
                State = NextCombatState();

                break;

            case CombatState.EnemyMidTurn:
                HandleEnemyMidTurnState();
                State = NextCombatState();

                break;

            case CombatState.EnemyPostTurn:
                HandleEnemyPostTurnState();
                State = NextCombatState();

                break;

            case CombatState.Win:
                HandleWinState();

                break;

            case CombatState.Lose:
                HandleLoseState();

                break;
        }
    }
}
