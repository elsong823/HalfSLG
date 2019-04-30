using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{

    // TiggerValue 形式战斗单位之间的信息 保存在brain中 每个人自己监听自己关注的人的
    public class DataPackRelation: IUpdateValues
    {
    
        public TriggerValue<int> damageFromTarget { get; private set; }
        public TriggerValue<int> damageToTarget { get; private set; }
        public TriggerValue<int> distance { get; private set; }

        public void UpdateValues(BattleFieldEvent battleAction)
        {

        }
    }

}
