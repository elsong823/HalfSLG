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
            m_name = name;
        }

        private string m_name;
        public string Name
        {
            get
            {
                return m_name;
            }
        }

        private object m_body;
        public object Body
        {
            get
            {
                return m_body;
            }
            set
            {
                m_body = value;
            }
        }

        public override string ToString()
        {
            return m_name;
        }
    }

}