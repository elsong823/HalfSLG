using System;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public interface IELPoolObject
    {
        bool PoolObjActive { get; set; }
    }

    public interface IRecyclable
    {
        void Recycle();
    }

    public class ELStack<T>
        where T : IELPoolObject, new()
    {
        private string key = string.Empty;
        //创建函数
        private Func<string, T> createFunction;
        //池
        private Stack<T> pool;

        public ELStack(int capacity, string key, Func<string, T> createFunction)
        {
            this.key = key;
            this.createFunction = createFunction;
            pool = new Stack<T>();

            for (int i = 0; i < capacity; ++i)
            {
                pool.Push(Create());
            }
        }

        private T Create()
        {
            if (createFunction != null)
                return createFunction(key);

            T obj = new T();
            return obj;
        }

        public T Get()
        {
            if (pool.Count == 0)
            {
                return Create();
            }

            return pool.Pop();
        }

        public void Return(T obj)
        {
            if (obj != null)
            {
                obj.PoolObjActive = false;
                pool.Push(obj);
            }
        }

        public int Count
        {
            get { return pool == null ? 0 : pool.Count; }
        }

        public void Clear()
        {
            if (pool != null)
                pool.Clear();
        }
    }

    //可被计数的对象
    public class CountableInstance
    {
        public int ID;
    }

    public class CounterMap<T, S>
        where T : IGameBase, new()
        where S : CountableInstance, new()
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
        protected S Create()
        {
            S instance = new S();
            instance.ID = ++count;
            dic.Add(instance.ID, instance);

            return instance;
        }

        //根据id获取对象
        public S Get(int id)
        {
            S instance = default(S);
            dic.TryGetValue(id, out instance);
            return instance;
        }
    }

    public class SingletonDyncRecyclableList<T>
        where T : IRecyclable, new()
    {
        private int beUsed = 0;
        private List<T> data;

        public SingletonDyncRecyclableList(int capacity)
        {
            data = new List<T>(capacity);
        }

        //获取一个空的
        public T Get()
        {
            if (beUsed >= data.Count)
                data.Add(new T());

            ++beUsed;
            return data[beUsed - 1];
        }

        //重置
        public void Reset(int resetLength = -1)
        {
            if (data != null)
            {
                for (int i = 0; i < data.Count; ++i)
                    data[i].Recycle();

                //如果需要重置长度
                if (resetLength >= 0)
                    data.RemoveRange(0, data.Count - resetLength);

                //重新计算使用量
                beUsed = 0;
            }
        }

        //排序
        public void Sort(IComparer<T> comparer)
        {
            if(comparer == null)
            {
                UtilityHelper.LogError("SingletonDyncRecyclableList sort failed. None comparer!");
                return;
            }
            //排序
            data.Sort(0, beUsed, comparer);
        }

        //获取列表(有效的)
        public List<T> GetUsed()
        {
            if (beUsed > 0)
                return data.GetRange(0, beUsed);

            return null;
        }

        //获取第一个
        public T GetFirst()
        {
            return beUsed > 0 ? data[0] : default(T);
        }

        //已用数量
        public int BeUsedCount
        {
            get
            {
                return beUsed;
            }
        }
    }
}