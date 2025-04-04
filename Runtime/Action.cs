using UnityEngine.InputSystem;

using Moths.Inputs.Attributes;

namespace Moths.Inputs
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