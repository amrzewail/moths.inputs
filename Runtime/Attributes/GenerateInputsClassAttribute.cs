using System;
using UnityEngine;

namespace Moths.Inputs.Attributes
{

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateInputsClassAttribute : Attribute
    {
        public string Path { get; private set; }
        public GenerateInputsClassAttribute(string actionsPathRelativeToAssets)
        {
            Path = actionsPathRelativeToAssets;
        }
    }
}