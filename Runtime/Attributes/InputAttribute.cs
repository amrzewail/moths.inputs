using System;
using UnityEngine;
using static Moths.Inputs.InputController;

namespace Moths.Inputs
{
    public enum InputType
    {
        Axis,
        Button,
        Trigger,
    };

    public class InputAttribute : Attribute
    {
        public InputType type;
        public InputKey key;

        public InputAttribute(InputType type, string key)
        {
            this.type = type;
            this.key = key;
        }
    }
}