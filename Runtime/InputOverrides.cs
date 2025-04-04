using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Moths.Inputs.Utilities;

using Moths.Attributes;

namespace Moths.Inputs
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Inputs/Input Overrides")]
    [PreserveScriptableObject]
    public class InputOverrides : ScriptableObject
    {
        [System.Serializable]
        public struct InputOverride
        {
            public string bindingPath;
            public InputAction inputAction;
        }

        [SerializeField] InputActionAsset asset;
        [SerializeField] List<InputOverride> dirtyOverrides;

        private InputOverride[] _overrides;


        public bool isRebinding { get; private set; }

        public bool HasOverrides => _overrides != null && _overrides.Length > 0;

        public event System.Action RebindingStarted;
        public event System.Action RebindingCompleted;
        public event System.Action RebindingCanceled;

        public void Apply(InputActionAsset asset)
        {
            asset.RemoveAllBindingOverrides();

            _overrides = new InputOverride[dirtyOverrides.Count];
            for (int i = 0; i < _overrides.Length; i++)
            {
                _overrides[i] = dirtyOverrides[i];

                var @override = _overrides[i];

                BindingPath bindingPath = @override.bindingPath;

                var assetAction = asset.FindAction(bindingPath.Action);

                if (assetAction == null) continue;

                var scheme = bindingPath.Scheme;

                if (@override.inputAction.bindings.Count == 0) continue;

                if (!bindingPath.IsComposite)
                {
                    var bindingIndex = assetAction.GetBindingIndex(bindingPath.Scheme);
                    if (bindingIndex == -1) continue;
                    var newBinding = @override.inputAction.bindings[0];
                    assetAction.ApplyBindingOverride(bindingIndex, newBinding.path);
                }
                else
                {
                    var compositeBinding = assetAction.ChangeBinding(bindingPath.Composite);
                    if (compositeBinding.bindingIndex == -1) continue;
                    if (compositeBinding.binding.isComposite)
                    {
                        var newBinding = @override.inputAction.bindings[0];
                        var binding = compositeBinding.NextPartBinding(bindingPath.Part);
                        if (!string.IsNullOrEmpty(newBinding.effectivePath))
                        {
                            assetAction.ApplyBindingOverride(newBinding.effectivePath, group: bindingPath.Scheme, path: binding.binding.path);
                        }
                    }
                }
            }
        }

        public void RevertDirty()
        {
            dirtyOverrides = new List<InputOverride>(_overrides.Length);
            dirtyOverrides.AddRange(_overrides);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void RevertAllRebinds(InputActionAsset asset)
        {
            asset.RemoveAllBindingOverrides();
            dirtyOverrides.Clear();
            _overrides = new InputOverride[0];
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void RevertRebind(BindingPath path)
        {
            var pathStr = path.ToString();
            for (int i = 0; i < dirtyOverrides.Count; i++)
            {
                var o = dirtyOverrides[i];
                if (o.bindingPath == pathStr)
                {
                    dirtyOverrides.RemoveAt(i);
                    break;
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public InputBinding GetDirtyBinding(BindingPath bindingPath)
        {
            var action = FindAction(bindingPath);
            if (action != null)
            {
                if (action.bindings.Count != 0)
                {
                    return action.bindings[0];
                }
            }

            if (!asset)
            {
                Debug.LogError("Input Overrides: Asset not found");
                return default;
            }

            action = asset.FindAction(bindingPath.Action);

            if (InputUtility.FindBinding(action, bindingPath, out var binding, out int bindingIndex))
            {
                return binding;
            }

            return default;
        }


        public async Task ListenForRebind(BindingPath path)
        {
            InputAction action = CreateMissingActionOrScheme(path);

            if (action == null) return;

            await Task.Delay(100);

            Debug.Log($"Start rebinding of action: {path.Action}");

            InputBinding? currentBinding = null;
            if (action.bindings.Count > 0)
            {
                currentBinding = action.bindings[0];
            }

            bool isCompleted = false;
            bool isCanceled = false;

            var operation = action.PerformInteractiveRebinding()
                .WithBindingGroup(path.Scheme)
                //.OnMatchWaitForAnother(0.1f)
                .WithExpectedControlType("Button")
                .OnCancel(op =>
                {
                    if (currentBinding != null)
                    {
                        action.ChangeBinding(0).To(currentBinding.Value);
                    }
                    else
                    {
                        RevertRebind(path);
                    }
                    isCanceled = true;
                    isCompleted = true;
                })
                .OnComplete(op =>
                {
                    switch (action.bindings[action.bindings.Count - 1].effectivePath)
                    {
                        case "<Keyboard>/escape":

                        case "<Gamepad>/start":
                        case "<Gamepad>/leftStick/left":
                        case "<Gamepad>/leftStick/right":
                        case "<Gamepad>/leftStick/up":
                        case "<Gamepad>/leftStick/down":
                        case "<Gamepad>/rightStick/left":
                        case "<Gamepad>/rightStick/right":
                        case "<Gamepad>/rightStick/up":
                        case "<Gamepad>/rightStick/down":

                        case "<XInputController>/start":
                        case "<XInputController>/leftStick/left":
                        case "<XInputController>/leftStick/right":
                        case "<XInputController>/leftStick/up":
                        case "<XInputController>/leftStick/down":
                        case "<XInputController>/rightStick/left":
                        case "<XInputController>/rightStick/right":
                        case "<XInputController>/rightStick/up":
                        case "<XInputController>/rightStick/down":

                            if (currentBinding != null)
                            {
                                action.ChangeBinding(action.bindings.Count - 1).To(currentBinding.Value);
                            }
                            else
                            {
                                RevertRebind(path);
                            }
                            isCanceled = true;
                            break;
                    }
                    if (action.bindings.Count > 1)
                    {
                        action.ChangeBinding(0).Erase();
                    }
                    isCompleted = true;
                });

            for (int i = 0; i < asset.controlSchemes.Count; i++)
            {
                if (asset.controlSchemes[i].name != path.Scheme) continue;
                var deviceCount = asset.controlSchemes[i].deviceRequirements.Count;
                for (int j = 0; j < deviceCount; j++)
                {
                    operation.WithControlsHavingToMatchPath(asset.controlSchemes[i].deviceRequirements[j].controlPath);
                }
            }


            operation.WithRebindAddingNewBinding(path.Scheme);

            isRebinding = true;
            RebindingStarted?.Invoke();

            operation.Start();

            while (!isCompleted)
            {
                if (!Application.isPlaying) return;
                await Task.Delay(100);
            }

            operation.Dispose();


            isRebinding = false;

            if (isCanceled)
            {
                Debug.Log($"Canceled rebinding of action: {path.Action}");
                RebindingCanceled?.Invoke();
                return;
            }

            RebindingCompleted?.Invoke();

            Debug.Log($"Completed rebinding of: {path} with key: {action.bindings[0].effectivePath} displayName:{action.bindings[0].ToDisplayString()}");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private InputAction FindAction(BindingPath path)
        {
            var pathStr = path.ToString();
            for (int i = 0; i < dirtyOverrides.Count; i++)
            {
                if (dirtyOverrides[i].bindingPath == pathStr)
                {
                    return dirtyOverrides[i].inputAction;
                }
            }
            return null;
        }


        public InputAction CreateMissingActionOrScheme(BindingPath path)
        {
            var pathStr = path.ToString();
            for (int i = 0; i < dirtyOverrides.Count; i++)
            {
                if (dirtyOverrides[i].bindingPath == pathStr)
                {
                    return dirtyOverrides[i].inputAction;
                }
            }

            var newOverride = new InputOverride();
            newOverride.bindingPath = pathStr;
            newOverride.inputAction = new InputAction();
            dirtyOverrides.Add(newOverride);
            return newOverride.inputAction;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public void FromJson(string json)
        {
            InputOverrides overrides = ScriptableObject.CreateInstance<InputOverrides>();
            JsonUtility.FromJsonOverwrite(json, overrides);
            this.dirtyOverrides = overrides.dirtyOverrides;
        }

    }
}