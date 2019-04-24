using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.AI
{
    public interface IBattleActionCreator
    {
        BattleFieldEvent CreateBattleAction();
    }
}
