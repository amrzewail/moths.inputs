using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moths.Inputs
{
    [System.Serializable]
    public struct BindingPath
    {
        [SerializeField] string _actionAsset;

        [SerializeField] string _scheme;
        [SerializeField] string _action;
        [SerializeField] string _composite;
        [SerializeField] string _part;

        public string Action => _action;
        public string Scheme => _scheme;
        public string Composite => _composite;
        public string Part => _part;

        public bool IsComposite => !string.IsNullOrEmpty(Composite);

        public BindingPath(string scheme, string action, string composite = "", string part = "")
        {
            _actionAsset = null;
            _action = action;
            _scheme = scheme;
            _composite = composite;
            _part = part;
        }

        public BindingPath(string bindingPath)
        {
            this = bindingPath;
        }

        /// <summary>
        /// Scheme/Action/Composite/Part
        /// Composite and Part are optional
        /// </summary>

        public static implicit operator BindingPath(string path)
        {
            string[] splits = path.Split('/');

            if (splits.Length == 1)
            {
                return new BindingPath("", splits[0], "", "");
            }

            string scheme = splits[0];
            string action = splits[1];
            string composite = "";
            string part = "";
            if (splits.Length > 2) composite = splits[2];
            if (splits.Length > 3) part = splits[3];
            return new BindingPath(scheme, action, composite, part);
        }

        public override string ToString()
        {
            string value = $"{Scheme}/{Action}";
            if (!string.IsNullOrEmpty(Composite))
            {
                value += $"/{Composite}";
            }
            if (!string.IsNullOrEmpty(Part))
            {
                value += $"/{Part}";
            }
            return value;
        }
    }
}