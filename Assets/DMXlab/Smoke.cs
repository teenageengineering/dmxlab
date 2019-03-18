using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DMXlab
{
    [ExecuteInEditMode]
    public class Smoke : MonoBehaviour
    {
        [Range(0, 1)]
        public float amount;

        float _prevAmount;

        void Update()
        {
            if (amount == _prevAmount)
                return;

            foreach (LightShafts lightShaft in FindObjectsOfType<LightShafts>())
                lightShaft.m_Brightness = amount;

            _prevAmount = amount;
        }
    }
}