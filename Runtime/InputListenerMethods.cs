using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Inputs.InputController;

namespace Inputs
{
    public struct AxisParams
    {
        public Vector2 value;

        public AxisParams(float x, float y) => value = new Vector2(x, y);
        public AxisParams(Vector2 v) => value = v;

        public static implicit operator Vector2(AxisParams axis) => axis.value;

        public override string ToString() => value.ToString();
    }

    public struct ButtonParams
    {
        public ButtonState state;
    }

    public struct TriggerParams
    {
        public bool isDown;
    }

    public struct InputListenerMethods
    {
        public delegate void AxisMethod(AxisParams p);
        public delegate void ButtonMethod(ButtonParams p);
        public delegate void TriggerMethod(TriggerParams p);

        public Dictionary<Guid, List<AxisMethod>> axisMethods;
        public Dictionary<Guid, List<ButtonMethod>> buttonMethods;
        public Dictionary<Guid, List<TriggerMethod>> triggerMethods;

        private IInputListener _listener;

        private static Dictionary<Type, MethodInfo[]> _typeMethodCache = new Dictionary<Type, MethodInfo[]>();

        public override int GetHashCode()
        {
            if (_listener == null) return 0;
            return _listener.GetHashCode();
        }

        public static bool operator == (InputListenerMethods m1, InputListenerMethods m2)
        {
            return m1._listener == m2._listener;
        }

        public static bool operator !=(InputListenerMethods m1, InputListenerMethods m2) => !(m1 == m2);

        public override bool Equals(object obj)
        {
            if (obj is InputListenerMethods)
            {
                return (InputListenerMethods)obj == this;
            }
            return base.Equals(obj);
        }

        public static InputListenerMethods HashKey(IInputListener listener) => new InputListenerMethods { _listener = listener };

        public InputListenerMethods(IInputListener listener)
        {
            this = default;

            _listener = listener;

            var type = listener.GetType();

            if (!_typeMethodCache.ContainsKey(type))
            {
                _typeMethodCache[type] = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            MethodInfo[] methods = _typeMethodCache[type];

            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var attr = method.GetCustomAttribute<InputAttribute>();
                if (attr == null) continue;
                switch (attr.type)
                {
                    case InputType.Axis:
                        if (axisMethods == null) axisMethods = new Dictionary<Guid, List<AxisMethod>>();
                        AddToDict(axisMethods, attr.key.Guid, listener, method);
                        break;
                    case InputType.Button:
                        if (buttonMethods == null) buttonMethods = new Dictionary<Guid, List<ButtonMethod>>();
                        AddToDict(buttonMethods, attr.key.Guid, listener, method);
                        break;
                    case InputType.Trigger:
                        if (triggerMethods == null) triggerMethods = new Dictionary<Guid, List<TriggerMethod>>();
                        AddToDict(triggerMethods, attr.key.Guid, listener, method);
                        break;
                }
            }
        }

        private void AddToDict<TDelegate>(Dictionary<Guid, List<TDelegate>> methods, Guid key, IInputListener instance, MethodInfo method) where TDelegate : System.Delegate
        {
            if (!methods.ContainsKey(key)) methods[key] = new List<TDelegate>();
            var del = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), instance, method);
            methods[key].Add(del);
        }
    }
}