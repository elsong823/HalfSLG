using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public interface IGameEvent
    {
        string Name { get; }

        object Body { get; }

        string ToString();
    }
}