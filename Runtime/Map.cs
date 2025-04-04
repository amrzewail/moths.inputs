using Moths.Inputs.Attributes;
using System.Collections.Generic;

namespace Moths.Inputs
{
    [System.Serializable]
    public struct Map
    {
        [ReadOnly]
        public string name;

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Map) return ((Map)obj).name.Equals(name);
            return base.Equals(obj);
        }

        public List<Action> actions;
    }
}