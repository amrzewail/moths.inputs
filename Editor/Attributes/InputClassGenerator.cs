using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Type = System.Type;

using Moths.Inputs.Attributes;
using Moths.Inputs;

namespace Moths.Inputs.Editor
{

    public class InputClassGenerator : AssetPostprocessor
    {
        private static List<string> addedAssets = new List<string>();
        private static List<string> delAssets = new List<string>();

        static async void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            await System.Threading.Tasks.Task.Delay(300);

            var types = TypeCache.GetTypesWithAttribute<GenerateInputsClassAttribute>();

            addedAssets.Clear();
            delAssets.Clear();

            addedAssets.AddRange(importedAssets);
            addedAssets.AddRange(movedAssets);

            delAssets.AddRange(deletedAssets);
            delAssets.AddRange(movedFromAssetPaths);

            for (int i = 0; i < types.Count; i++)
            {
                var typeName = types[i].Name;

                string[] res = System.IO.Directory.GetFiles(Application.dataPath, $"{typeName}.cs", SearchOption.AllDirectories);
                if (res.Length == 0) continue;

                GenerateInputsClassAttribute attr = (GenerateInputsClassAttribute)types[i].GetCustomAttributes(typeof(GenerateInputsClassAttribute), false)[0];

                var newAssets = addedAssets.Where(asset => asset.Contains(attr.Path)).ToArray();
                var removedAssets = delAssets.Where(asset => asset.Contains(attr.Path)).ToArray();

                if (newAssets.Length == 0 && removedAssets.Length == 0) continue;

                for (int j = 0; j < res.Length; j++)
                {
                    string scriptFullPath = res[j];

                    if (!File.ReadAllText(scriptFullPath).Contains("[GenerateInputsClass(\"")) continue;

                    scriptFullPath = scriptFullPath.Replace("\\", "/");

                    //var assetsIndex = scriptFullPath.IndexOf("Assets/");

                    //string scriptPath = scriptFullPath.Replace("\\", "/").Substring(assetsIndex, scriptFullPath.Length - assetsIndex);

                    RegenerateScript(types[i], scriptFullPath, attr.Path);
                }


            }
        }

        private static void RegenerateScript(Type type, string scriptPath, string assetPath)
        {
            var scriptText = File.ReadAllText(scriptPath);
            var originalScriptText = scriptText;

            scriptText.Replace("\n\n", "\n");

            var openingBraceIndex = scriptText.IndexOf($"class {type.Name}", scriptText.IndexOf("[GenerateInputsClass("));

            scriptText = scriptText.Remove(openingBraceIndex + $"class {type.Name}".Length);

            Script script = new Script(scriptText);

            assetPath = "Assets/" + assetPath;

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

            if (!asset)
            {
                script.Save(scriptPath);
                Debug.LogError($"{nameof(InputClassGenerator)}::RegenerateScript Error: Asset: {assetPath} not found.");
                return;
            }

            script.Body.AddComment("This script is auto generated by InputClassGenerator");
            script.Body.AddNewLine();

            for (int i = 0; i < asset.actionMaps.Count; i++)
            {
                var map = asset.actionMaps[i];
                var mapName = Regex.Replace(map.name.Replace(" ", ""), "\\[[^\\]]+\\]", "");
                Body structMap = new Body($"public struct {mapName}");

                for (int j = 0; j < map.actions.Count; j++)
                {
                    var action = map.actions[j];

                    var actionName = action.name.Replace(" ", "");
                    actionName = Regex.Replace(actionName, "\\[[^\\]]+\\]", "");

                    structMap.AddConstant(Protection.Public, $"{actionName}_str", $"{mapName}/{actionName}_{action.id}");
                    structMap.AddStaticReadonly(Protection.Public, nameof(InputKey), actionName, (FieldName)$"{actionName}_str");
                    structMap.AddNewLine();
                }

                script.Body.AddBody(structMap);
                script.Body.AddNewLine();
            }

            if (originalScriptText.Replace("\n", "").Equals(script.Code.Replace("\n", ""))) return;

            script.Save(scriptPath);
        }

        private enum Protection
        {
            Public,
            Private,
            Protected,
            Internal,
        };

        private enum Binding
        {
            Member,
            Static,
            Const,
        };

        private enum Mutability
        {
            Mutable,
            Readonly
        };


        private struct Body
        {
            private StringBuilder _text;
            public Body(string prefix)
            {
                _text = new StringBuilder(prefix);
                _text.Append(" {\n\n\t");
            }

            private string ProtectionString(Protection protection)
            {
                return protection switch { Protection.Public => "public", Protection.Private => "private", Protection.Protected => "protected", Protection.Internal => "internal", _ => "" };
            }
            private string BindingString(Binding binding) => binding switch { Binding.Member => "", Binding.Static => "static", Binding.Const => "const", _ => "" };
            private string MutabilityString(Mutability mutability) => mutability switch { Mutability.Mutable => "", Mutability.Readonly => "readonly", _ => "" };

            private void AddField(Protection protection, Binding binding, Mutability mutability, string type, string name, object value)
            {
                _text.Append(ProtectionString(protection));
                _text.Append(" ");
                _text.Append(BindingString(binding));
                if (binding != Binding.Member) _text.Append(" ");
                if (binding != Binding.Const)
                {
                    _text.Append(MutabilityString(mutability));
                    if (mutability != Mutability.Mutable) _text.Append(" ");
                }
                _text.Append(type);
                _text.Append(" ");
                _text.Append(name);
                if (value != null)
                {
                    _text.Append(" = ");
                    if (value is string)
                    {
                        _text.Append($"\"{value}\"");
                    }
                    else if (value is float?)
                    {
                        _text.Append($"{value}f");
                    }
                    else if (value is FieldName)
                    {
                        _text.Append($"{value}");
                    }
                    else
                    {
                        _text.Append(value);
                    }
                }
                _text.Append(";\n\t");
            }

            public string Code => _text.ToString() + "\n}";
            public void AddOpeningBrace() => _text.Append("{\n");
            public void AddClosingBrace() => _text.Append("}\n");
            public void AddNewLine() => _text.Append("\n\t");
            public void AddComment(string comment) => _text.Append($"// {comment}\n\t");

            public void AddConstant(Protection protection, string name, string value) => AddField(protection, Binding.Const, Mutability.Mutable, "string", name, value);
            public void AddConstant(Protection protection, string name, float? value) => AddField(protection, Binding.Const, Mutability.Mutable, "float", name, value);
            public void AddConstant(Protection protection, string name, int? value) => AddField(protection, Binding.Const, Mutability.Mutable, "int", name, value);

            public void AddVariable(Protection protection, string type, string name, object value) => AddField(protection, Binding.Member, Mutability.Mutable, type, name, value);
            public void AddVariable(Protection protection, string name, string value) => AddVariable(protection, "string", name, value);
            public void AddVariable(Protection protection, string name, int? value) => AddVariable(protection, "int", name, value);
            public void AddVariable(Protection protection, string name, float? value) => AddVariable(protection, "float", name, value);

            public void AddStaticReadonly(Protection protection, string type, string name, object value) => AddField(protection, Binding.Static, Mutability.Readonly, type, name, value);
            public void AddStaticReadonly(Protection protection, string name, string value) => AddStaticReadonly(protection, "string", name, value);
            public void AddStaticReadonly(Protection protection, string name, float? value) => AddStaticReadonly(protection, "float", name, value);
            public void AddStaticReadonly(Protection protection, string name, int? value) => AddStaticReadonly(protection, "int", name, value);


            public void AddBody(Body body) => _text.Append($"{body.Code.Replace("\n", "\n\t")}\n\t");
        }

        private struct FieldName
        {
            public string value;
            public static implicit operator FieldName(string value) => new FieldName { value = value };
            public override string ToString() => value;
        }

        private struct Script
        {

            private Body _body;

            public Body Body => _body;
            public string Code => _body.Code;

            public Script(string text)
            {
                _body = new Body(text);
            }

            public void Save(string scriptPath)
            {
                File.WriteAllText(scriptPath, this);
            }

            public static implicit operator string(Script script) => script.Code;

        }
    }
}