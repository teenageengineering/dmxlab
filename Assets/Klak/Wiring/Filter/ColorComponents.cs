using UnityEngine;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Convertion/Color Components")]
    public class ColorComponents : NodeBase
    {
        #region Node I/O

        [Inlet]
        public Color color {
            set {
                if (!enabled) 
                    return;

                _redEvent.Invoke(value.r);
                _greenEvent.Invoke(value.g);
                _blueEvent.Invoke(value.b);
                _alphaEvent.Invoke(value.a);
            }
        }

        [SerializeField, Outlet]
        FloatEvent _redEvent = new FloatEvent();

        [SerializeField, Outlet]
        FloatEvent _greenEvent = new FloatEvent();

        [SerializeField, Outlet]
        FloatEvent _blueEvent = new FloatEvent();

        [SerializeField, Outlet]
        FloatEvent _alphaEvent = new FloatEvent();

        #endregion
    }
}
