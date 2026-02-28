using UnityEngine;

namespace Moths.Inputs
{
    public class InputControllerOwner : MonoBehaviour
    {
        [SerializeField] InputController _inputs;

        public void Enable()
        {
            _inputs?.Enable(this);
        }

        public void Disable()
        {
            _inputs?.Disable(this);
        }

        public void MaskEnable()
        {
            _inputs?.MaskEnable(this);
        }

        private void OnDestroy()
        {
            _inputs?.Disable(this);
        }
    }
}
