using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Inputs.Editor
{
    [CustomPropertyDrawer(typeof(BindingPath))]
    public class BindingPathPropertyEditor : PropertyDrawer
    {
        List<string> _dropdownChoices = new List<string>(128);
        List<string> _actionChoices = new List<string>(32);
        InputActionAsset[] _inputActionAssets = null;
        List<InputAction> _inputActions = new List<InputAction>(32);
        StringBuilder _str = new StringBuilder(64);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var actionAssetProb = property.FindPropertyRelative("_actionAsset");
            var schemeProb = property.FindPropertyRelative("_scheme");
            var actionProb = property.FindPropertyRelative("_action");
            var compositeProb = property.FindPropertyRelative("_composite");
            var partProb = property.FindPropertyRelative("_part");

            BindingPath bindingPath = new BindingPath(schemeProb.stringValue, actionProb.stringValue, compositeProb.stringValue, partProb.stringValue);

            string currentFullBindingPath = $"{actionAssetProb.stringValue}/{bindingPath}";

            var container = new VisualElement();

            container.style.display = DisplayStyle.Flex;
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.paddingLeft = 3;
            container.AddToClassList(BaseField<VisualElement>.alignedFieldUssClassName);

            var label = new Label(property.displayName);
            label.AddToClassList(BaseField<Label>.labelUssClassName);
            //label.style.width = Length.Percent(40);

            container.Add(label);

            if (_inputActionAssets == null)
            {
                _inputActionAssets = FindAssets<InputActionAsset>(out _);
            }

            _dropdownChoices.Clear();

            int dropdownSelectedIndex = 0;

            for (int i = 0; i < _inputActionAssets.Length; i++)
            {
                _actionChoices.Clear();
                var asset = _inputActionAssets[i];

                SetActions(asset, _inputActions);

                for (int j = 0; j < _inputActions.Count; j++) AddActionChoices(_inputActions[j], asset.name, _actionChoices);

                if (_actionChoices.Contains(currentFullBindingPath))
                {
                    dropdownSelectedIndex = _dropdownChoices.Count + _actionChoices.IndexOf(currentFullBindingPath);
                }

                _dropdownChoices.AddRange(_actionChoices);
            }

            var dropdown = new DropdownField(_dropdownChoices, dropdownSelectedIndex);

            dropdown.RegisterValueChangedCallback(ev =>
            {
                var bindingPath = new BindingPath(ev.newValue.Substring(ev.newValue.IndexOf('/') + 1));

                actionAssetProb.stringValue = ev.newValue.Substring(0, ev.newValue.IndexOf('/'));
                schemeProb.stringValue = bindingPath.Scheme;
                actionProb.stringValue = bindingPath.Action;
                compositeProb.stringValue = bindingPath.Composite;
                partProb.stringValue = bindingPath.Part;

                property.serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(property.serializedObject.targetObject);
            });

            dropdown.style.width = Length.Percent(60);

            container.Add(dropdown);

            return container;
        }

        private void SetActions(InputActionAsset asset, List<InputAction> output)
        {
            output.Clear();
            for (int i = 0; i < asset.actionMaps.Count; i++)
            {
                var map = asset.actionMaps[i];

                output.AddRange(map.actions);
            }
        }

        private void AddActionChoices(InputAction action, string prefix, List<string> choices)
        {
            string currentComposite = null;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (!binding.isPartOfComposite) currentComposite = null;

                if (binding.isComposite)
                {
                    currentComposite = binding.name;
                    continue;
                }


                _str.Clear();

                _str.Append(prefix);

                _str.Append('/');
                _str.Append(binding.groups);

                _str.Append("/");
                _str.Append(binding.action);
                
                if (binding.isPartOfComposite && !string.IsNullOrEmpty(currentComposite)) 
                {
                    _str.Append('/');
                    _str.Append(currentComposite);
                }
                
                if (!string.IsNullOrEmpty(binding.name))
                {
                    _str.Append('/');
                    _str.Append(binding.name);
                }

                choices.Add(_str.ToString());
            }
        }


        static T[] FindAssets<T>(out string[] guids) where T : Object
        {
            string search = $"t:{typeof(T).Name}";

            guids = AssetDatabase.FindAssets(search);
            var assets = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return assets;
        }
    }
}