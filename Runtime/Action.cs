using UnityEngine.InputSystem;

using Inputs.Attributes;

namespace Inputs
{
    [System.Serializable]
    public struct Action
    {
        [ReadOnly]
        public string name;

        public InputActionReference reference;

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}