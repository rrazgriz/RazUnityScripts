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
        // Validate that there's at least one transform selected
        [MenuItem("GameObject/Reset Transform Without Affecting Children", true)]
        static bool CanResetTransform() => Selection.transforms.Length >= 1;

        // Reset the transform of the selected GameObjects without affecting the pose of their children
        [MenuItem("GameObject/Reset Transform Without Affecting Children", false, 0)]
        static void ResetTransform()
        {
            // Create a temporary parent object to store the children
            Transform tempTransform = new GameObject("__ResetTransformWithoutAffectingChildren_Temp").transform;
            tempTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            tempTransform.localScale = Vector3.one;

            // Iterate through all selected transforms
            foreach(Transform transform in Selection.transforms)
            {
                // Iterate through the children of our selection and parent them to the temporary parent, keeping their world orientation
                foreach(Transform child in transform)
                {
                    Undo.RecordObject(child, "Reset Transform Without Affecting Children");
                    child.transform.SetParent(tempTransform, true);
                }

                // Reset the transform of the parent object
                Undo.RecordObject(transform, "Reset Transform Without Affecting Children");
                transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                transform.localScale = Vector3.one;

                // Iterate through the children and restore their world orientation
                foreach(Transform child in tempTransform)
                {
                    child.transform.SetParent(transform, true);
                }
            }

            // Remove the temporary parent object
            Object.DestroyImmediate(tempTransform.gameObject);
        }
    }
}
#endif