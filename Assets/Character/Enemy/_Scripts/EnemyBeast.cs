using System.Collections.Generic;
using UnityEngine;

public class EnemyBeast : EnemyController {
    public string Name = "Beast";

    public EnemyBeast() {
        MaxHealth = 50;
        Health = 50;
    }

    private Dictionary<string, ActionInterface> _Actions = new Dictionary<string, ActionInterface>(){
        {ActionHeavyAttack.NAME, new ActionHeavyAttack()},
        {ActionLightAttack.NAME, new ActionLightAttack()}
    };

    public override ActionInterface DecideAction() {
        List<string> attacks = new List<string>(){
            ActionLightAttack.NAME,
            ActionLightAttack.NAME,
            ActionHeavyAttack.NAME
        };

        string action = attacks[Random.Range(0, attacks.Count)];
        QueuedAction = _Actions[action];

        return QueuedAction;
    }

}
