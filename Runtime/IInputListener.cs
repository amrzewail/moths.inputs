using System.Reflection;
using UnityEngine;

namespace Moths.Inputs
{
    public interface IInputListener 
    {
        void OnInputLostFocus(InputController input) { }

    }
}