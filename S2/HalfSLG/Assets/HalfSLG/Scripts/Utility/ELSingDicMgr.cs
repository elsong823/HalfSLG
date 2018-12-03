using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//单例，带字典的泛型类
namespace ELGame
{
    public class ELSingDicMgr<T, S> 
        where T : IELBase, new() 
        where S : new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                    instance.Init();
                }
                return instance;
            }
        }

        protected int count = 0;
        protected Dictionary<int, S> dic = new Dictionary<int, S>();

        //创建并增加计数
        protected void Create(out S instance, out int id)
        {
            instance = new S();
            id = count++;
            dic.Add(id, instance);
        }

        //根据id获取对象
        public S Get(int id)
        {
            S instance = default(S);
            dic.TryGetValue(id, out instance);
            return instance;
        }
    }
}