using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [CreateAssetMenu(menuName = "BBSystem/RecoveryChip")]
    public class BattleRecoveryChip
        : BattleStrategyChip
    {
        public override string ChipName => "RecoveryChip";

        public override BattleBehaviourType BehaviourType => BattleBehaviourType.Recovery;

        public override bool NeedRecordSkillResult => false;

        public override void CalculateBehaviourItem(List<BattleBehaviourItem> behaviourItems, float weight)
        {
            BattleUnit battleUnit = null;

            for(int i = 0; i < baseData.ownBattleTeam.battleUnits.Count; ++i)
            {
                battleUnit = baseData.ownBattleTeam.battleUnits[i];
                //别晃了！他已经...他已经...走了....
                if (!battleUnit.CanAction)
                    continue;

                BattleBehaviourItem behaviourItem = BattleBehaviourItem.CreateInstance(battleUnit, BehaviourType);

                //计算剩余血量
                float hpLost = 1f - (float)battleUnit.battleUnitAttribute.hp / battleUnit.battleUnitAttribute.maxHp;
                //插值计算
                //规则：损失 >= 60% -> 1f
                //     损失 <= 20% -> 0f
                float recoverWeight = Mathf.Lerp(0f, 0.95f, (hpLost - 0.2f) * 2.5f) + Mathf.Lerp(0, 0.05f, (hpLost - 0.6f) / 0.4f);

                //距离
                int distance = baseData.hostBattleUnit.mapGrid.Distance(battleUnit.mapGrid);
                float distanceWeight = behaviourSystem.GetDistanceWeight(distance);

                behaviourItem.point = EGameConstL.BattleBehaviourChipMaxPoint * recoverWeight * distanceWeight * weight;

                battleBehaviourItems.Add(behaviourItem);
            }

            AddToTargetList(behaviourItems);
        }

        public override void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult)
        {
        }

        public override void ResetChip() { }
    }
}