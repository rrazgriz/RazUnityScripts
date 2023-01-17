// Regenerates GUIDs for Unity assets, and replaces all references to them in the scene.
// Drop into Assets/Editor or Assets/Scripts, Select Assets in project view, right click, `Regenerate GUIDs/Regenerate`
// `Regenerate GUIDs/Regenerate Recursive` will recursively regenerate for asset in any folders selected.
// Please know that this can break things, and only use it if you know you have a use case for changing GUIDs in-place.

// License Unknown
// Original script from https://gist.github.com/ZimM-LostPolygon/7e2f8a3e5a1be183ac19

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UnityGuidRegenerator {
    public class UnityGuidRegeneratorMenu {
        [MenuItem("Assets/Regenerate GUIDs/Regenerate")]
        public static void RegenerateGuids() {
            if (EditorUtility.DisplayDialog("GUIDs regeneration",
                "You are going to start the process of GUID regeneration. This may have unexpected results. \n\nMAKE A PROJECT BACKUP BEFORE PROCEEDING!",
                "Regenerate GUIDs", "Cancel")) {
                try {
                    AssetDatabase.StartAssetEditing();

                    string path = Path.GetFullPath(".");

                    UnityGuidRegenerator regenerator = new UnityGuidRegenerator(path);
                    regenerator.RegenerateGuids(false);
                }
                finally {
                    AssetDatabase.StopAssetEditing();
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();
                }
            }
        }

        [MenuItem("Assets/Regenerate GUIDs/Regenerate (Recursive)")]
        public static void RegenerateGuidsRecursive() {
            if (EditorUtility.DisplayDialog("GUIDs regeneration",
                "You are going to start the process of GUID regeneration. This may have unexpected results. \n\nMAKE A PROJECT BACKUP BEFORE PROCEEDING!",
                "Regenerate GUIDs", "Cancel")) {
                try {
                    AssetDatabase.StartAssetEditing();

                    string path = Path.GetFullPath(".") + Path.DirectorySeparatorChar;

                    UnityGuidRegenerator regenerator = new UnityGuidRegenerator(path);
                    regenerator.RegenerateGuids(true);
                }
                finally {
                    AssetDatabase.StopAssetEditing();
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();
                }
            }
        }
    }

    internal class UnityGuidRegenerator {
        private static readonly string[] kDefaultFileExtensions = {
            "*.meta",
            "*.mat",
            "*.anim",
            "*.prefab",
            "*.unity",
            "*.asset",
            "*.guiskin",
            "*.fontsettings",
            "*.controller",
        };

        private readonly string _assetsPath;
        private readonly string _projectPath;

        public UnityGuidRegenerator(string projectPath) {
            _projectPath = projectPath;
            _assetsPath = projectPath + Path.DirectorySeparatorChar + "Assets";
        }

        public void RegenerateGuids(bool recursiveFolders = false) {

            List<string> selectedGuids = Selection.assetGUIDs.ToList();
            List<string> guidsToRegenerate = new List<string>();

            foreach (string guid in selectedGuids) {
                if (Path.GetExtension(AssetDatabase.GUIDToAssetPath(guid)) != ".meta") {
                    guidsToRegenerate.Add(guid);
                }

                if(recursiveFolders) {
                    if (Directory.Exists(MakeAbsolutePath(AssetDatabase.GUIDToAssetPath(guid), _projectPath))) {
                        string[] filesToIterateOver = Directory.GetFiles(AssetDatabase.GUIDToAssetPath(guid), "*", SearchOption.AllDirectories);

                        foreach (string file in filesToIterateOver) {
                            if (Path.GetExtension(file) != ".meta") {
                                guidsToRegenerate.Add(AssetDatabase.AssetPathToGUID(file));
                            }
                        }
                    }
                }
            }

            string[] regeneratedExtensions = kDefaultFileExtensions;

            // Get list of working files
            List<string> filesPaths = new List<string>();
            foreach (string extension in regeneratedExtensions) {
                filesPaths.AddRange(
                    Directory.GetFiles(_assetsPath, extension, SearchOption.AllDirectories)
                    );
            }

            // Create dictionary to hold old-to-new GUID map
            Dictionary<string, string> guidOldToNewMap = new Dictionary<string, string>();
            Dictionary<string, List<string>> guidsInFileMap = new Dictionary<string, List<string>>();

            // We must only replace GUIDs for Resources present in Assets. 
            // Otherwise built-in resources (shader, meshes etc) get overwritten.
            HashSet<string> ownGuids = new HashSet<string>();

            // Traverse all files, remember which GUIDs are in which files and generate new GUIDs
            int counter = 0;
            int filesPathsCount = filesPaths.Count;
            foreach (string filePath in filesPaths) {
                if (!EditorUtility.DisplayCancelableProgressBar("Scanning Assets folder", MakeRelativePath(_assetsPath, filePath),
                    counter / (float) filesPathsCount)) {
                    string contents = File.ReadAllText(filePath);

                    IEnumerable<string> guids = GetGuids(contents);
                    bool isFirstGuid = true;
                    foreach (string oldGuid in guids) {
                        // First GUID in .meta file is always the GUID of the asset itself
                        if (isFirstGuid && Path.GetExtension(filePath) == ".meta") {
                            ownGuids.Add(oldGuid);
                            isFirstGuid = false;
                        }
                        // Generate and save new GUID if we haven't added it before
                        if (!guidOldToNewMap.ContainsKey(oldGuid)) {
                            string newGuid = Guid.NewGuid().ToString("N");
                            guidOldToNewMap.Add(oldGuid, newGuid);
                        }

                        if (!guidsInFileMap.ContainsKey(filePath))
                            guidsInFileMap[filePath] = new List<string>();

                        if (!guidsInFileMap[filePath].Contains(oldGuid)) {
                            guidsInFileMap[filePath].Add(oldGuid);
                        }
                    }

                    counter++;
                } else {
                    UnityEngine.Debug.LogWarning("GUID regeneration canceled");
                    return;
                }
            }

            // Traverse the files again and replace the old GUIDs
            counter = -1;
            int guidsInFileMapKeysCount = guidsInFileMap.Keys.Count;
            foreach (string filePath in guidsInFileMap.Keys) {
                EditorUtility.DisplayProgressBar("Regenerating GUIDs", MakeRelativePath(_assetsPath, filePath), counter / (float) guidsInFileMapKeysCount);
                counter++;

                string contents = File.ReadAllText(filePath);
                foreach (string oldGuid in guidsInFileMap[filePath]) {
                    if (!ownGuids.Contains(oldGuid))
                        continue;
                    
                    if(!guidsToRegenerate.Contains(oldGuid)) {
                        continue;
                    }

                    string newGuid = guidOldToNewMap[oldGuid];
                    if (string.IsNullOrEmpty(newGuid))
                        throw new NullReferenceException("newGuid == null");
                    if (contents.Contains(oldGuid)) {
                        UnityEngine.Debug.Log(MakeRelativePath(_assetsPath, filePath) + " : Replacing GUID " + oldGuid + " with " + newGuid);
                    }
                    contents = contents.Replace("guid: " + oldGuid, "guid: " + newGuid);
                }
                File.WriteAllText(filePath, contents);
            }

            EditorUtility.ClearProgressBar();
        }

        private static IEnumerable<string> GetGuids(string text) {
            const string guidStart = "guid: ";
            const int guidLength = 32;
            int textLength = text.Length;
            int guidStartLength = guidStart.Length;
            List<string> guids = new List<string>();

            int index = 0;
            while (index + guidStartLength + guidLength < textLength) {
                index = text.IndexOf(guidStart, index, StringComparison.Ordinal);
                if (index == -1)
                    break;

                index += guidStartLength;
                string guid = text.Substring(index, guidLength);
                index += guidLength;

                if (IsGuid(guid)) {
                    guids.Add(guid);
                }
            }

            return guids;
        }

        private static bool IsGuid(string text) {
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (
                    !((c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'z'))
                    )
                    return false;
            }

            return true;
        }

        private static string MakeRelativePath(string fromPath, string toPath) {
            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }

        private static string MakeAbsolutePath(string relativePath, string basePath = null) {
            return basePath + Path.DirectorySeparatorChar + relativePath;
        }
    }
}