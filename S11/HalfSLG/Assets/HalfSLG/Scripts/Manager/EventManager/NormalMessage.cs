using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class NormalMessage 
        : IGameEvent
    {
        public NormalMessage(string name)
        {
            this.name = name;
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
        }

        private object body;
        public object Body
        {
            get
            {
                return body;
            }
            set
            {
                body = value;
            }
        }

        public override string ToString()
        {
            return name;
        }
    }

}