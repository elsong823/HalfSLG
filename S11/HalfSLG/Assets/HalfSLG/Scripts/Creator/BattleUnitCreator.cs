
using UnityEngine;

namespace ELGame
{
    public class BattleUnitCreator
        : CounterMap<BattleUnitCreator, BattleUnit>, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            BattleManager.Instance.MgrLog("Battle unit data creator inited.");
        }

        public BattleUnit Create(SO_BattleUnitAttribute battleUnitAttribute)
        {
            BattleUnit battleUnit = Create();

            battleUnit.battleUnitAttribute = GameObject.Instantiate<SO_BattleUnitAttribute>(battleUnitAttribute);
            battleUnit.battleUnitAttribute.hostBattleUnit = battleUnit;
            if (!battleUnit.battleUnitAttribute.manualOperation)
                battleUnit.battleBehaviourSystem = GameObject.Instantiate<BattleBehaviourSystem.BattleBehaviourSystem>(battleUnitAttribute.battleBehaviourSystem);
            battleUnit.battleUnitAttribute.RandomAttributes();
            battleUnit.battleUnitAttribute.Reset();

            return battleUnit;
        }
    }
}