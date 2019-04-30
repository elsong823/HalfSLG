using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.Resource
{
    public interface IRecyclable
    {
        void OnRecycle();
    }

    public class RecycleBinItemComparer
        : IComparer<RecycleBinItem>
    {
        public int Compare(RecycleBinItem x, RecycleBinItem y)
        {
            if (x.Weight != y.Weight)
                return x.Weight - y.Weight;
            else
            {
                int value_X = (int)x.timeStamp;
                int value_Y = (int)y.timeStamp;
                return value_Y - value_X;
            }
        }
    }

    public class LiteSingleton<T>
        where T : new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }

                return instance;
            }
        }
    }

    public class LitePool<T>
        where T : IRecyclable, new()
    {
        private Stack<T> stack = new Stack<T>();

        public void Return(T t)
        {
            if (t != null)
                t.OnRecycle();

            stack.Push(t);
        }

        public T Get()
        {
            if (stack.Count == 0)
                return new T();

            return stack.Pop();
        }

        public int Size
        {
            get
            {
                return stack.Count;
            }
        }
    }

    public class ResourceUtility
    {
    }
}
