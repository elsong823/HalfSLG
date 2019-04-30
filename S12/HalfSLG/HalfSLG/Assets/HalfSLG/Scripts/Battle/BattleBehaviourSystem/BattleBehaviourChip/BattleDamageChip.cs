//计算伤害某个目标
//会考虑当前仇恨，受仇恨芯片影响

using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [CreateAssetMenu(menuName = "BBSystem/CommonDamageChip")]
    public class BattleDamageChip 
        : BattleStrategyChip
    {
        public override string ChipName => "DamageChip";
        public override BattleBehaviourType BehaviourType => BattleBehaviourType.Damage;
        public override bool NeedRecordSkillResult => false;
        
        public override void CalculateBehaviourItem(List<BattleBehaviourItem> behaviourItems, float weight)
        {
            BattleUnit battleUnit = null;
            
            for (int i = 0; i < baseData.enemyBattleTeam.battleUnits.Count; ++i)
            {
                battleUnit = baseData.enemyBattleTeam.battleUnits[i];
                if (!battleUnit.CanAction)
                    continue;

                BattleBehaviourItem item = BattleBehaviourItem.CreateInstance(battleUnit, BehaviourType);
                int distance = baseData.hostBattleUnit.mapGrid.Distance(battleUnit.mapGrid);
                float distanceWeight = behaviourSystem.GetDistanceWeight(distance);
                item.point = EGameConstL.BattleBehaviourChipMaxPoint * weight * distanceWeight;

                battleBehaviourItems.Add(item);
            }

            //调整行为分数
            AddToTargetList(behaviourItems);
        }

        public override void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult)
        {

        }

        public override void ResetChip() { }
    }
}