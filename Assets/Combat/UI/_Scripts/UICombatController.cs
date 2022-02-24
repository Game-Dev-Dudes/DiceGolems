using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum UIState {
    Selected,
    Deselected,
    None
}

public class UICombatController : MonoBehaviour {
    public GameObject Player;
    public GameObject Enemy;
    public Canvas HUD;
    public Text PlayerHealth;
    public Text EnemyName;
    public Text EnemyHealth;
    public Text EnemyAction;
    public Text WinLose;
    public Text EnergyLevel;
    public Transform DicePool;
    public Die SelectedDie;
    public RectTransform TileContainer;
    public RectTransform TileTemplate;
    public RectTransform DiceTemplate;

    private PlayerCombatController _PlayerCombatController;
    private Enemy _Enemy;
    private ZoneCombatController _ZonesController;
    private StateCombatController _StateController;
    private UIState _SelectedState = UIState.Deselected;

    public void Start() {
        _StateController = GetComponent<StateCombatController>();
        _PlayerCombatController = Player.GetComponent<PlayerCombatController>();
        _Enemy = Enemy.GetComponent<Enemy>();
        _ZonesController = GetComponent<ZoneCombatController>();
    }

    public void SetEnemyName(string text) {
        EnemyName.text = text;
    }

    public void UpdatePlayerHealth() {
        PlayerHealth.text = _PlayerCombatController.Health + "/" + _PlayerCombatController.MaxHealth;
    }

    public void UpdateEnemyHealth() {
        EnemyHealth.text = _Enemy.Health + "/" + _Enemy.MaxHealth;
    }

    public void UpdateEnemyAction(string action) {
        EnemyAction.text = EnemyName.text + " is going to use " + action.ToUpper();
    }

    public void UpdateWinLose(string text, Color color) {
        WinLose.text = text;
        WinLose.color = color;
    }

    public void GetEnergy() {
        EnergyLevel.text = _PlayerCombatController.GetEnergy().ToString();

        if (_PlayerCombatController.GetEnergy() == 0) {
            EnergyLevel.color = Color.red;
        } else {
            EnergyLevel.color = Color.white;
        }
    }

    public void LoadTiles() {
        float offset = 0;
        float padding = 20;

        foreach (Tile tile in _PlayerCombatController.Tiles.Values) {
            RectTransform newTile = Instantiate<RectTransform>(TileTemplate, TileContainer, false);
            newTile.localPosition = new Vector2(-offset, 0);

            newTile.GetComponent<UICombatTile>().TileUUID = tile.UUID;
            LoadTileDiceSlots(newTile, tile.DiceSlots);

            _ZonesController.AddZone(tile.UUID);
            newTile.Find("Send").GetComponent<Button>().onClick.AddListener(() => ActivateTile(newTile.Find("DicePlaceholder")));

            offset += newTile.sizeDelta.x + padding;
        }

        _ZonesController.AddZone(DicePool.GetComponent<UICombatTile>().TileUUID);
    }

    public void LoadTileDiceSlots(RectTransform tile, int slots) {
        RectTransform tileDiceContainer = tile.Find("DicePlaceholder") as RectTransform;
        float offset = tileDiceContainer.sizeDelta.x / (slots + 1);

        for (int i = 0; i < slots; i++) {
            RectTransform newDice = Instantiate<RectTransform>(DiceTemplate);
            newDice.SetParent(tile.Find("DicePlaceholder"), false);
            newDice.localPosition = new Vector2(-offset * (i + 1), 0);

            newDice.GetComponent<Button>().onClick.AddListener(() => MoveDice(newDice.GetComponent<UICombatDiceSlot>()));
        }
    }

    public void RollDice() {
        if (_PlayerCombatController.GetEnergy() > 0) {
            UICombatDiceSlot poolSlot = GetPoolDiceSlot("");

            if (poolSlot == null) {
                return;
            }
            Die rolledDie = _PlayerCombatController.GenerateDice();

            poolSlot.Set(rolledDie);
            _ZonesController.AddDie(DicePool.GetComponent<UICombatTile>().TileUUID, rolledDie);
        }

        GetEnergy();
    }

    public void MoveDice(UICombatDiceSlot slot) {
        switch (_SelectedState) {
            case UIState.Selected:
                SetSlot(slot);
                break;
            case UIState.Deselected:
                SelectDice(slot);
                break;
        }
    }

    private void SelectDice(UICombatDiceSlot diceSlot) {
        if (_SelectedState == UIState.Selected || diceSlot.dieUUID == "") {
            return;
        }

        SelectedDie = _ZonesController.GetDie(diceSlot.dieUUID);

        _SelectedState = UIState.Selected;
    }

    private void SetSlot(UICombatDiceSlot toSlot) {
        if (_SelectedState != UIState.Selected) {
            return;
        }

        string toZone = toSlot.GetComponentInParent<UICombatTile>().TileUUID;

        Die tempSlot = _ZonesController.GetDie(toSlot.dieUUID);
        UICombatDiceSlot fromSlot = GetAllDiceSlot(SelectedDie.UUID);

        if (tempSlot == null) {
            _ZonesController.MoveDie(toZone, SelectedDie.UUID);
        } else {
            _ZonesController.SwapDice(tempSlot.UUID, SelectedDie.UUID);
        }

        string fromZone = fromSlot.GetComponentInParent<UICombatTile>().TileUUID;
        fromSlot.Clear();

        toSlot.Set(SelectedDie);

        if (tempSlot != null) {
            fromSlot.Set(tempSlot);
        }

        SelectedDie = null;
        _SelectedState = UIState.Deselected;
    }

    public void ActivateTile(Transform slotParent) {
        string tileZone = slotParent.GetComponentInParent<UICombatTile>().TileUUID;
        int dieSum = 0;

        List<Die> dice = new List<Die>();
        foreach (UICombatDiceSlot slot in slotParent.GetComponentsInChildren<UICombatDiceSlot>()) {
            if (!System.String.IsNullOrEmpty(slot.dieUUID)) {
                Die die = _ZonesController.GetDie(slot.dieUUID);
                dieSum += die != null ? die.Value : 0;

                dice.Add(die);

                _ZonesController.RemoveDie(slot.dieUUID);
                slot.Clear();
            }
        }

        Tile tile = _PlayerCombatController.Tiles[tileZone];

        System.Type t = TileUtility.TileOverrideDict[tile.TileName];
        TileOverride o = System.Activator.CreateInstance(t) as TileOverride;
        o.Execute(_Enemy, _PlayerCombatController, dice, tile);

        UpdateEnemyHealth();
        UpdatePlayerHealth();
    }

    public void EndTurn() {
        foreach (UICombatDiceSlot diceSlot in HUD.GetComponentsInChildren<UICombatDiceSlot>()) {
            diceSlot.Clear();
        }

        _StateController.IsPlayerTurnEnded = true;
    }

    private UICombatDiceSlot GetPoolDiceSlot(string uuid) {
        foreach (UICombatDiceSlot diceSlot in DicePool.GetComponentsInChildren<UICombatDiceSlot>()) {
            if (uuid == diceSlot.dieUUID) {
                return diceSlot;
            }
        }

        return null;
    }

    private UICombatDiceSlot GetAllDiceSlot(string uuid) {
        foreach (UICombatDiceSlot diceSlot in HUD.GetComponentsInChildren<UICombatDiceSlot>()) {
            if (uuid == diceSlot.dieUUID) {
                return diceSlot;
            }
        }
        return null;
    }
}