using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleSkillManager
        : BaseManager<BattleSkillManager>
    {
        public override string MgrName => "BattleSkillManager";

        private Dictionary<int, SO_BattleSkill> dicBattleSkills = new Dictionary<int, SO_BattleSkill>();
        
        public override void InitManager()
        {
            base.InitManager();
            InitSkills();
        }
        
        //初始化技能
        private void InitSkills()
        {
            Object[] skills = GetAssetsFromBundle("scriptableobjects/battleskill.unity3d", typeof(SO_BattleSkill));
            if (skills != null)
            {
                for (int i = 0; i < skills.Length; ++i)
                {
                    SO_BattleSkill skill = skills[i] as SO_BattleSkill;
                    if (skill == null)
                        continue;

                    dicBattleSkills.Add(skill.skillID, skill);
                }
            }
        }

        //获取技能
        public SO_BattleSkill GetSkill(int skillID)
        {
            if(!dicBattleSkills.ContainsKey(skillID))
            {
                UtilityHelper.LogError(string.Format("Get skill by id failed -> {0}", skillID));
                return null;
            }
            return dicBattleSkills[skillID];
        }
    }
}