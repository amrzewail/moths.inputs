using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Moths.Inputs.Attributes;
using System.Reflection;
using System;
using Moths.Attributes;

namespace Moths.Inputs
{
    public enum State
    {
        Disabled,
        Enabled,
        Mask,
    };

    [System.Flags]
    public enum Device
    {
        Keyboard = 1,
        Gamepad = 2,
    };

    [PreserveScriptableObject]
    [CreateAssetMenu(menuName = "ScriptableObjects/Inputs/Input Controller")]
    public partial class InputController : ScriptableObject
    {
        public enum ButtonState 
        {
            Down,
            Up,
        };

        [SerializeField] int _priority = 0;

        [SerializeField] Device _enabledDevices = Device.Keyboard | Device.Gamepad;
        [SerializeField] State _state;

        [SerializeField] List<InputActionReference> _axis2D;
        [SerializeField] List<InputActionReference> _buttons;
        [SerializeField] List<InputActionReference> _triggers;

        private static List<InputController> _enabledInputs = new List<InputController>();

        private static Dictionary<IInputListener, InputListenerMethods> _totalListeners;

        private HashSet<InputListenerMethods> _listenersHashSet;
        private List<InputListenerMethods> _listeners;

        public State State => _state;

        public UnityEvent<InputActionReference, ButtonParams> OnButton;
        public UnityEvent<InputActionReference, AxisParams> OnAxis2D;
        public UnityEvent<InputActionReference, TriggerParams> OnTrigger;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initalize()
        {
            _enabledInputs = new List<InputController>();
            _totalListeners = new Dictionary<IInputListener, InputListenerMethods>();

        }

        public void Enable()
        {
            if (_state == State.Enabled) return;

            Disable();
            _state = State.Enabled;
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].action.performed += ButtonPerformedCallback;
                _buttons[i].action.canceled += ButtonPerformedCallback;
                _buttons[i].action.Enable();
            }
            for (int i = 0; i < _axis2D.Count; i++)
            {
                _axis2D[i].action.performed += Axis2DPerformedCallback;
                _axis2D[i].action.canceled += Axis2DPerformedCallback;
                OnAxis2D?.Invoke(_axis2D[i], new AxisParams(Vector2.zero));
                _axis2D[i].action.Enable();
            }
            for (int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].action.performed += TriggerPerformedCallback;
                _triggers[i].action.canceled += TriggerCanceledCallback;
                _triggers[i].action.Enable();
            }

            if (!_enabledInputs.Contains(this))
            {
                _enabledInputs.Add(this);
            }

            _enabledInputs.Sort((x, y) => x._priority.CompareTo(y._priority));

            if (_enabledInputs[^1] == this)
            {
                for (int i = 0; i < _enabledInputs.Count - 1; i++)
                {
                    _enabledInputs[i].LostFocus();
                }
            }
        }

        public void MaskEnable()
        {
            if (_state == State.Mask) return;

            Disable();
            Enable();

            _state = State.Mask;
        }

        public void Disable()
        {
            if (_state == State.Disabled) return;

            _state = State.Disabled;
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].action.performed -= ButtonPerformedCallback;
                _buttons[i].action.canceled -= ButtonPerformedCallback;
            }
            for (int i = 0; i < _axis2D.Count; i++)
            {
                _axis2D[i].action.performed -= Axis2DPerformedCallback;
                _axis2D[i].action.canceled -= Axis2DPerformedCallback;
            }
            for (int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].action.performed -= TriggerPerformedCallback;
                _triggers[i].action.canceled -= TriggerCanceledCallback;
            }

            _enabledInputs.Remove(this);
        }

        private delegate void InputAxis(AxisParams axis);

        public void RegisterListener(IInputListener listener)
        {
            if (_totalListeners == null) return;

            if (_listenersHashSet == null || _listeners == null)
            {
                _listenersHashSet = new HashSet<InputListenerMethods>();
                _listeners = new List<InputListenerMethods>();
            }

            if (!_totalListeners.ContainsKey(listener))
            {
                _totalListeners[listener] = new InputListenerMethods(listener);
            }

            var l = _totalListeners[listener];

            if (!_listenersHashSet.Contains(l))
            {
                _listeners.Add(l);
                _listenersHashSet.Add(l);
            }
        }

        public void UnregisterListener(IInputListener listener)
        {
            if (_totalListeners == null) return;
            var l = _totalListeners[listener];
            if (!_listenersHashSet.Contains(l)) return;
            _listenersHashSet.Remove(l);
            _listeners.Remove(l);
        }

        private bool HasControl(InputControl control)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                var controls = _buttons[i].action.controls;
                for (int j = 0; j < controls.Count; j++)
                {
                    if (controls[j].device == control.device && controls[j].path == control.path) return true;
                }
            }

            for (int i = 0; i < _axis2D.Count; i++)
            {
                var controls = _axis2D[i].action.controls;
                for (int j = 0; j < controls.Count; j++)
                {
                    if (controls[j].device == control.device && controls[j].path == control.path) return true;
                }
            }

            for (int i = 0; i < _triggers.Count; i++)
            {
                var controls = _triggers[i].action.controls;
                for (int j = 0; j < controls.Count; j++)
                {
                    if (controls[j].device == control.device && controls[j].path == control.path) return true;
                }
            }

            return false;
        }

        private void LostFocus()
        {
            for (int i = 0; i < _listeners.Count; i++) _listeners[i].Listener.OnInputLostFocus(this);
        }

        private bool IsEnabled(InputAction.CallbackContext ctx)
        {
            if (_state == State.Disabled) return false;
            if (_enabledInputs.Count == 0) return false;
            if ((ctx.control.device is Keyboard || ctx.control.device is Mouse) && !_enabledDevices.HasFlag(Device.Keyboard)) return false;
            if (ctx.control.device is Gamepad && !_enabledDevices.HasFlag(Device.Gamepad)) return false;
            return true;
        }

        private bool IsOverriden(InputControl control)
        {
            int myIndex = -1;
            for (int i = 0; i < _enabledInputs.Count; i++)
            {
                if (myIndex == -1)
                {
                    if (_enabledInputs[i] == this) myIndex = i;
                    continue;
                }
                else
                {
                    if (_enabledInputs[i].State == State.Enabled) return true;

                    if (_enabledInputs[i].State == State.Mask)
                    {
                        if (_enabledInputs[i].HasControl(control)) return true;
                    }
                }
            }

            return false;
        }

        private InputActionReference GetInputActionReference(Guid id, List<InputActionReference> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].action.id == id)
                {
                    return list[i];
                }
            }
            return null;
        }

        private void Axis2DPerformedCallback(InputAction.CallbackContext ctx)
        {
            if (!IsEnabled(ctx) || IsOverriden(ctx.control)) return;

            Vector2 axis = ctx.ReadValue<Vector2>();

            var p = new AxisParams(axis);

            var action = GetInputActionReference(ctx.action.id, _axis2D);
            OnAxis2D?.Invoke(action, p);

            if (_listeners == null) return;
            for (int i = 0; i < _listeners.Count; i++)
            {
                var listener = _listeners[i];
                if (listener.axisMethods == null) continue;
                if (listener.axisMethods.TryGetValue(ctx.action.id, out var methods))
                {
                    for (int j = 0; j < methods.Count; j++) methods[j](p);
                }
            }
        }

        private void ButtonPerformedCallback(InputAction.CallbackContext ctx)
        {
            if (!IsEnabled(ctx) || IsOverriden(ctx.control)) return;

            ButtonParams p = new ButtonParams { state = ctx.performed ? ButtonState.Down : ButtonState.Up };

            var action = GetInputActionReference(ctx.action.id, _buttons);
            OnButton?.Invoke(action, p);

            if (_listeners == null) return;
            for (int i = 0; i < _listeners.Count; i++)
            {
                var listener = _listeners[i];
                if (listener.buttonMethods == null) continue;
                if (listener.buttonMethods.TryGetValue(ctx.action.id, out var methods))
                {
                    for (int j = 0; j < methods.Count; j++) methods[j](p);
                }
            }
        }

        private void TriggerPerformedCallback(InputAction.CallbackContext ctx)
        {
            if (!IsEnabled(ctx) || IsOverriden(ctx.control)) return;

            var p = new TriggerParams { isDown = true };

            var action = GetInputActionReference(ctx.action.id, _triggers);
            OnTrigger?.Invoke(action, p);

            if (_listeners == null) return;
            for (int i = 0; i < _listeners.Count; i++)
            {
                var listener = _listeners[i];
                if (listener.triggerMethods == null) continue;
                if (listener.triggerMethods.TryGetValue(ctx.action.id, out var methods))
                {
                    for (int j = 0; j < methods.Count; j++) methods[j](p);
                }
            }
        }

        private void TriggerCanceledCallback(InputAction.CallbackContext ctx)
        {
            if (!IsEnabled(ctx) || IsOverriden(ctx.control)) return;

            var p = new TriggerParams { isDown = false };

            var action = GetInputActionReference(ctx.action.id, _triggers);
            OnTrigger?.Invoke(action, p);

            if (_listeners == null) return;
            for (int i = 0; i < _listeners.Count; i++)
            {
                var listener = _listeners[i];
                if (listener.triggerMethods == null) continue;
                if (listener.triggerMethods.TryGetValue(ctx.action.id, out var methods))
                {
                    for (int j = 0; j < methods.Count; j++) methods[j](p);
                }
            }
        }

    }
}