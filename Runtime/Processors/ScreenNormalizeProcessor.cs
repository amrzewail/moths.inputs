using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Inputs.Processors
{
#if UNITY_EDITOR
    using UnityEditor;
    [InitializeOnLoad]
#endif
    public class ScreenNormalizeProcessor : InputProcessor<Vector2>
    {
#if UNITY_EDITOR
        static ScreenNormalizeProcessor()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<ScreenNormalizeProcessor>();
        }


        public override Vector2 Process(Vector2 value, InputControl control)
        {
            Vector2 normalized = value;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            normalized /= screen.magnitude;
            return normalized;
        }
    }
}