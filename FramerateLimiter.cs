// License Unknown
// Based on LLeaLoo's script

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Raz
{
    public class FramerateLimiter : MonoBehaviour
    {
        public int targetFrameRate = 30;
    }

    [CustomEditor(typeof(FramerateLimiter), true)]
    public class FramerateLimiterEditor : Editor
    {
        #if UNITY_EDITOR
        private FramerateLimiter my;

        public override void OnInspectorGUI()
        {
            my = (FramerateLimiter) target;

            this.DrawDefaultInspector();
            GUILayout.Label("Current Target Framerate: " + Application.targetFrameRate.ToString());

            if (GUILayout.Button("Update Target")){ SetTargetFramerate(); }
        }

        void SetTargetFramerate()
        {
            my = (FramerateLimiter) target;

            QualitySettings.vSyncCount = 0;  // VSync must be disabled
            Application.targetFrameRate = my.targetFrameRate; // Use -1 to set back to default
        }
        #endif
    }
}
