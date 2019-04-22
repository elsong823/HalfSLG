using System.Collections.Generic;

namespace ELGame.BattleBehaviourSystem
{
    public interface IBattleBehaviourChip
    {
        //芯片名称
        string ChipName { get; }
        //类型
        BattleBehaviourType BehaviourType { get; }
        //是否需要记录战斗结果
        bool NeedRecordSkillResult { get; }

        //初始化
        void Init(BattleBehaviourSystem behaviourSystem, BattleBaseData baseData);

        //计算行为
        void CalculateBehaviourItem(List<BattleBehaviourItem> behaviourItems, float weight);
        
        //记录技能效果
        void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult);

        //重置
        void ResetChip();
    }
}