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
#if UNITY_EDITOR
            string[] files = System.IO.Directory.GetFiles(
                string.Format("{0}/HalfSLG/ScriptableObjects/BattleSkill", Application.dataPath), "*.asset", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var item in files)
            {
                string file = UtilityHelper.ConverToRelativePath(item.Replace("\\", "/"));
                SO_BattleSkill skill = UnityEditor.AssetDatabase.LoadAssetAtPath<SO_BattleSkill>(file);
                if (skill != null)
                    dicBattleSkills.Add(skill.skillID, skill);
            }
#endif
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