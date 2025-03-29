using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Inputs.Attributes;

namespace Inputs
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

        public State State => _state;

        public UnityEvent<InputActionReference, ButtonState> OnButton;
        public UnityEvent<InputActionReference, Vector2> OnAxis2D;
        public UnityEvent<InputActionReference, bool> OnTrigger;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initalize()
        {
            //Actions = new InputActions();
            //Actions.Enable();

            _enabledInputs = new List<InputController>();

            //foreach (var action in Actions.asset.actionMaps[0].actions)
            //{
            //    action.performed += (InputAction.CallbackContext ctx) =>
            //    {
            //        Debug.Log($"gameplay: {ctx.action.name}");
            //    };
            //}

            //foreach (var action in Actions.asset.actionMaps[1].actions)
            //{
            //    action.performed += (InputAction.CallbackContext ctx) =>
            //    {
            //        Debug.Log($"ui: {ctx.action.name}");
            //    };
            //}

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
            }
            for (int i = 0; i < _axis2D.Count; i++)
            {
                _axis2D[i].action.performed += Axis2DPerformedCallback;
                _axis2D[i].action.canceled += Axis2DPerformedCallback;
                OnAxis2D?.Invoke(_axis2D[i], Vector2.zero);
            }
            for (int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].action.performed += TriggerPerformedCallback;
                _triggers[i].action.canceled += TriggerCanceledCallback;
            }

            if (!_enabledInputs.Contains(this))
            {
                _enabledInputs.Add(this);
            }

            _enabledInputs.Sort((x, y) => x._priority.CompareTo(y._priority));
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

        private bool IsAllowedToWork(InputAction.CallbackContext ctx)
        {
            if (_state == State.Disabled) return false;

            if (_enabledInputs.Count == 0) return false;

            if ((ctx.control.device is Keyboard || ctx.control.device is Mouse) && !_enabledDevices.HasFlag(Device.Keyboard)) return false;
            if (ctx.control.device is Gamepad && !_enabledDevices.HasFlag(Device.Gamepad)) return false;

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
                    if (_enabledInputs[i].State == State.Enabled) return false;

                    if (_enabledInputs[i].State == State.Mask)
                    {
                        if (_enabledInputs[i].HasControl(ctx.control)) return false;
                    }
                }
            }

            return true;
        }

        private void ButtonPerformedCallback(InputAction.CallbackContext ctx)
        {
            if (!IsAllowedToWork(ctx)) return;

            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i].action.id == ctx.action.id)
                {
                    OnButton?.Invoke(_buttons[i], ctx.performed ? ButtonState.Down : ButtonState.Up);
                    break;
                }
            }
        }

        private void Axis2DPerformedCallback(InputAction.CallbackContext ctx)
        {
            if (!IsAllowedToWork(ctx)) return;

            for (int i = 0; i < _axis2D.Count; i++)
            {
                if (_axis2D[i].action.id == ctx.action.id)
                {
                    OnAxis2D?.Invoke(_axis2D[i], ctx.ReadValue<Vector2>());
                    break;
                }
            }
        }
        private void TriggerPerformedCallback(InputAction.CallbackContext ctx)
        {
            if (!IsAllowedToWork(ctx)) return;

            for (int i = 0; i < _triggers.Count; i++)
            {
                if (_triggers[i].action.id == ctx.action.id)
                {
                    OnTrigger?.Invoke(_triggers[i], true);
                    break;
                }
            }
        }
        private void TriggerCanceledCallback(InputAction.CallbackContext ctx)
        {
            if (!IsAllowedToWork(ctx)) return;

            for (int i = 0; i < _triggers.Count; i++)
            {
                if (_triggers[i].action.id == ctx.action.id)
                {
                    OnTrigger?.Invoke(_triggers[i], false);
                    break;
                }
            }
        }

    }
}