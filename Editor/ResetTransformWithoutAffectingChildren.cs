// Reset the transform of the selected GameObjects without affecting the pose of their children
// Use at your own risk. I tried to make it pretty safe (with undo capability), but I can't guarantee it won't break your scene.
// SPDX-License-Identifier: MIT
// Copyright 2022 Razgriz

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Raz
{
    public class ResetTransformWithoutAffectingChildren
    {
        [MenuItem("GameObject/Reset Transform Without Affecting Children", true)]
        static bool CanResetTransform() => Selection.transforms.Length >= 1;

        [MenuItem("GameObject/Reset Transform Without Affecting Children", false, 0)]
        static void ResetTransform()
        {
            // Create a temporary parent object to store the children
            Transform t = new GameObject("__temp").transform;
            t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;

            foreach(Transform transform in Selection.transforms)
            {
                // Iterate through the children and store them in the temporary parent, keeping their world orientation
                foreach(Transform child in transform)
                {
                    Undo.RecordObject(child, "Reset Transform Without Affecting Children");
                    child.transform.SetParent(t, true);
                }

                // Reset the transform of the parent object
                Undo.RecordObject(transform, "Reset Transform Without Affecting Children");
                transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                transform.localScale = Vector3.one;

                // Iterate through the children and restore their world orientation
                foreach(Transform child in t)
                {
                    child.transform.SetParent(transform, true);
                }
            }

            // Remove the temporary parent object
            Object.DestroyImmediate(t.gameObject);
        }
    }
}
#endif