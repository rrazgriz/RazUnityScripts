// Copy GUID
// SPDX-License-Identifier: MIT
// Copyright 2022 Razgriz

// Instructions: Download script, add to any editor scripts folder (folder named Editor in Assets folder).
// Usage: right click an object(s) or folder(s), select "Copy GUID(s) to Clipboard"
// Note: Will annotate selections with >1 object, since otherwise it's not very helpful

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Raz
{
    public class CopyGuidToClipboard
    {
        [MenuItem("Assets/Copy GUID(s) to Clipboard", true)]
        static bool CanCopyGuid() => Selection.assetGUIDs.Length >= 1;

        [MenuItem("Assets/Copy GUID(s) to Clipboard", false, 999)]
        static void CopyGuid()
        {
            if(Selection.assetGUIDs.Length == 1)
            {
                GUIUtility.systemCopyBuffer = Selection.assetGUIDs?[0];
            }
            else if(Selection.assetGUIDs.Length > 1)
            {
                string clipboardOutput = "";

                foreach(Object obj in Selection.objects)
                {
                    string path = AssetDatabase.GetAssetPath(obj);

                    // Probably not an asset selection (scene hierarchy), skip
                    if(path == "")
                        continue;

                    string guid = AssetDatabase.AssetPathToGUID(path);

                    if(Directory.Exists(path))
                        clipboardOutput += $"[Folder] {path} : {guid}\n";
                    else
                        clipboardOutput += $"{path} : {guid}\n";
                }

                if(clipboardOutput != "")
                    GUIUtility.systemCopyBuffer = clipboardOutput;
            }
        }
    }
}
#endif