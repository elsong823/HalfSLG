using System;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public interface IELPoolObject
    {
        bool PoolObjActive { get; set; }
    }

    public class ELStack<T>
        where T : IELPoolObject, new()
    {
        //创建函数
        private Func<T> createFunction;
        //池
        private Stack<T> pool;

        public ELStack(int capacity, Func<T> createFunction)
        {
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
                return createFunction();

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

    public class ELSingletonDic<T, S>
        where T : IGameBase, new()
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