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
    public class ScaleDeltaTime : InputProcessor<Vector2>
    {
#if UNITY_EDITOR
        static ScaleDeltaTime()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<ScaleDeltaTime>();
        }

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return value *= Time.deltaTime;
        }
    }
}