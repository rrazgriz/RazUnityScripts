// Play Mode Focus Handler
// SPDX-License-Identifier: MIT
// Copyright 2022 Razgriz

// Instructions: Download script, add to any editor scripts folder (folder named Editor in Assets folder).
// Usage: Tools -> Playmode Game View Focus to disable/enable this override.
// Notes:   
//  - The game view mode flashes for a couple frames before switching back to the correct window. Could be fixed with more invasive patching.
//  - Preference is stored in EditorPrefs, should apply across all projects that contain the script.
//  - Tested in Unity 2019.4.31f1, should work through Unity 2023 (not tested, but has the same function being reflected into)

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace Raz
{
	[InitializeOnLoad]
	public class PlayModeFocusHandler
	{
        static readonly Type windowLayoutType;
        static readonly MethodInfo tryGetLastFocusedWindowInSameDockMethod;
		static bool showGameViewOnPlayMode = false;

        const string TOOLBAR_PATH = "Tools/Playmode Game View Focus/";
		const string EDITOR_PREFS_KEY = "Raz.PlayModeFocusOverride";
		
        static PlayModeFocusHandler()
        {
			GetEditorPrefs();
            windowLayoutType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.WindowLayout");
            tryGetLastFocusedWindowInSameDockMethod = windowLayoutType.GetMethod("TryGetLastFocusedWindowInSameDock", BindingFlags.Static | BindingFlags.NonPublic); 
        }

		[InitializeOnEnterPlayMode]
		public static void OnEnterPlayMode()
		{
			EditorApplication.delayCall -= HandleGameModeRefocus;
			EditorApplication.delayCall += HandleGameModeRefocus;
		}

		static void HandleGameModeRefocus()
		{
			if(!showGameViewOnPlayMode)
				((EditorWindow)tryGetLastFocusedWindowInSameDockMethod?.Invoke(null, null))?.ShowTab();

			EditorApplication.delayCall -= HandleGameModeRefocus;
		}

        static void GetEditorPrefs() => showGameViewOnPlayMode = EditorPrefs.GetBool(EDITOR_PREFS_KEY, showGameViewOnPlayMode);
        static void SetEditorPrefs() => EditorPrefs.SetBool(EDITOR_PREFS_KEY, showGameViewOnPlayMode);

        [MenuItem(TOOLBAR_PATH + "Show GameView on PlayMode", true, 0)]
        static bool CanEnablePlayModeFocus() => showGameViewOnPlayMode == false;

        [MenuItem(TOOLBAR_PATH + "Show GameView on PlayMode", false, 0)]
        public static void EnablePlayModeFocus()
        {
            showGameViewOnPlayMode = true;
			SetEditorPrefs();
        }

        [MenuItem(TOOLBAR_PATH + "Don't show GameView on PlayMode", true, 0)]
        static bool CanDisablePlayModeFocus() => showGameViewOnPlayMode == true;
        
        [MenuItem(TOOLBAR_PATH + "Don't show GameView on PlayMode", false, 0)]
        public static void DisablePlayModeFocus()
        {
            showGameViewOnPlayMode = false;
			SetEditorPrefs();
        }
	}
}
#endif