//用于策略性激怒某个目标
//根据每个敌人实际制造的伤害来计算分数

using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [CreateAssetMenu(menuName = "BBSystem/ProvokeChip")]
    public class BattleProvokeChip 
        : BattleStrategyChip
    {
        public override string ChipName => "ProvokeChip";
        public override BattleBehaviourType BehaviourType => BattleBehaviourType.Provoke;
        public override bool NeedRecordSkillResult => true;

        protected BattleUnitPointRecorder damageRecorder = new BattleUnitPointRecorder();
        
        public override void Init(BattleBehaviourSystem behaviourSystem, BattleBaseData baseData)
        {
            base.Init(behaviourSystem, baseData);

            //建立伤害记录器
            damageRecorder.Init(baseData.enemyBattleTeam.battleUnits);
        }

        public void RecordDamage(BattleUnit maker, int damageAddition)
        {
            if (maker == null || damageAddition <= 0)
                return;

            damageRecorder.RecordAddition(maker, damageAddition);
        }

        public override void CalculateBehaviourItem(List<BattleBehaviourItem> behaviourItems, float weight)
        {
            var items = damageRecorder.Items(true, true);
            while (items.MoveNext())
            {
                BattleUnitRecordItem recordItem = items.Current;
                //这个家伙已经无法行动了
                if (!recordItem.maker.CanAction)
                    continue;

                var behaviourItem = BattleBehaviourItem.CreateInstance(recordItem.maker, BehaviourType);

                int distance = baseData.hostBattleUnit.mapGrid.Distance(recordItem.maker.mapGrid);
                float distanceWeight = behaviourSystem.GetDistanceWeight(distance);
                //判断自己是否当前目标的攻击对象
                float isHisTarget = 0.75f;
                if(recordItem.maker.targetBattleUnit != null)
                    isHisTarget = recordItem.maker.targetBattleUnit.Equals(baseData.hostBattleUnit) ? 0.5f : 1f;

                behaviourItem.point = recordItem.point * weight * distanceWeight * isHisTarget;

                battleBehaviourItems.Add(behaviourItem);
            }

            AddToTargetList(behaviourItems);
        }

        public override void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult)
        {
            //忽视我方队友
            if (!from.battleTeam.Equals(baseData.enemyBattleTeam))
                return;

            float hate = battleUnitSkillResult.syncAttribute.hpChanged > 0f ? battleUnitSkillResult.syncAttribute.hpChanged : -battleUnitSkillResult.syncAttribute.hpChanged;
            damageRecorder.RecordAddition(from, hate);
        }
        
        public override void ResetChip()
        {
            damageRecorder.Clear();
        }
    }
}