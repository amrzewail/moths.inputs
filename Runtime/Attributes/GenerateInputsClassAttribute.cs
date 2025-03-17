using System;
using UnityEngine;

namespace Inputs.Attributes
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