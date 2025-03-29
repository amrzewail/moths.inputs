using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Inputs.Utilities
{
    public static class InputUtility
    {
        private static double _lastDeviceUpdateTime => LastDevice == null ? Mathf.NegativeInfinity : LastDevice.lastUpdateTime;
        private static float _lastUpdateTime = -100;

        public static InputDevice LastDevice { get; private set; } = null;

        public static event Action<InputDevice> InputDeviceChanged;

        public static void ParseInputStringToSpriteAsset(StringBuilder builder, InputOverrides overrides)
        {
            try
            {
                BindingPath bindingPath = builder.ToString();

                if (string.IsNullOrEmpty(bindingPath.Scheme))
                {
                    if (LastDevice is Gamepad) bindingPath = new BindingPath("Controller", bindingPath.Action, bindingPath.Composite, bindingPath.Part);
                    else bindingPath = new BindingPath("Keyboard", bindingPath.Action, bindingPath.Composite, bindingPath.Part);
                }

                string str = overrides.GetDirtyBinding(bindingPath).ToDisplayString();

                if (bindingPath.Scheme == "Controller")
                {
                    if (LastDevice is UnityEngine.InputSystem.DualShock.DualShockGamepad)
                    {
                        str = $"<size=150%><sprite=\"dualshock\" name=\"{str}\"/></size>";
                    }
                    else
                    {
                        str = $"<size=150%><sprite=\"xbox\" name=\"{str}\"/></size>";
                    }
                }
                builder.Clear();
                builder.Append(str);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static bool FindBinding(InputAction action, BindingPath bindingPath, out InputBinding binding,  out int index)
        {
            index = -1;
            binding = default;
            if (bindingPath.IsComposite)
            {
                bool foundComposite = false;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    binding = action.bindings[i];
                    if (binding.isComposite && binding.name == bindingPath.Composite) foundComposite = true;
                    if (!foundComposite) continue;
                    if (!binding.isPartOfComposite) continue;
                    if (binding.name.ToLower() != bindingPath.Part.ToLower()) continue;
                    if (!binding.groups.Contains(bindingPath.Scheme)) continue;
                    index = i;
                    return true;
                }
            }
            else
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    binding = action.bindings[i];
                    if (binding.groups.Contains(bindingPath.Scheme))
                    {
                        index = i;
                        return true;
                    }
                }
            }
            
            return false;
        }


        [RuntimeInitializeOnLoadMethod]
        private static void AppStart()
        {
            InputDeviceChanged = null;

            var defaultSystems = PlayerLoop.GetDefaultPlayerLoop();
            var updateSystem = FindSubSystem(defaultSystems, typeof(PostLateUpdate.InputEndFrame));

            if (updateSystem.subSystemList == null) updateSystem.subSystemList = new PlayerLoopSystem[0];
            var updateSystemList = updateSystem.subSystemList.ToList();

            for (int i = 0; i < updateSystemList.Count; i++)
            {
                if (updateSystemList[i].type == typeof(InputUtility)) return;
            }

            updateSystemList.Add(new PlayerLoopSystem
            {
                updateDelegate = CustomUpdate,
                type = typeof(InputUtility)
            });

            updateSystem.subSystemList = updateSystemList.ToArray();

            ReplaceSystem<PostLateUpdate.InputEndFrame>(ref defaultSystems, updateSystem);

            PlayerLoop.SetPlayerLoop(defaultSystems);
        }

        private static void CustomUpdate()
        {
            if (!Application.isPlaying) return;

            InputSystem.Update();

            var device = LastDevice;

            if (Time.unscaledTime - _lastUpdateTime > 2)
            {
                var devices = InputSystem.devices;
                if (devices.Count == 0) return;
                LastDevice = devices[0];
                for (int i = 1; i < devices.Count; i++)
                {
                    if (devices[i].lastUpdateTime > _lastDeviceUpdateTime)
                    {
                        LastDevice = devices[i];
                    }
                }
                _lastUpdateTime = Time.time;
            }

            if (LastDevice != device)
            {
                InputDeviceChanged?.Invoke(LastDevice);
                Debug.Log($"Input device changed to: {LastDevice.name}");
            }
        }

        private static PlayerLoopSystem FindSubSystem(PlayerLoopSystem def, Type type)
        {
            if (def.type == type)
            {
                return def;
            }
            if (def.subSystemList != null)
            {
                foreach (var s in def.subSystemList)
                {
                    var system = FindSubSystem(s, type);
                    if (system.type == type)
                    {
                        return system;
                    }
                }
            }
            return default(PlayerLoopSystem);
        }

        private static bool ReplaceSystem<T>(ref PlayerLoopSystem system, PlayerLoopSystem replacement)
        {
            if (system.type == typeof(T))
            {
                system = replacement;
                return true;
            }
            if (system.subSystemList != null)
            {
                for (var i = 0; i < system.subSystemList.Length; i++)
                {
                    if (ReplaceSystem<T>(ref system.subSystemList[i], replacement))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}