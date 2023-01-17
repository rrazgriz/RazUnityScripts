// Set VR Player Settings (Unity 2019)
// SPDX-License-Identifier: MIT
// Copyright 2022 Razgriz

// Instructions: Download script, add to any folder in Unity Assets project.
// Usage: Tools -> VR Player Presets, select required mode. Files are saved in EditorPrefs, so should persist across projects.

// This script provides some quick options to apply specific VR SDK settings.
// May break in versions higher than 2019, as the VR SDK API is deprecated in favor of the XR Management tools

// Note:
// If patching the SDK was allowed, a quick harmony patch could be applied - code is left in for this.
// Since it's not allowed, I've commented that code, and instead just check and apply if changed on EditorApplication.update.

#if UNITY_EDITOR && !UNITY_2020_OR_HIGHER
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
// using HarmonyLib;

#pragma warning disable 0618 // SetVirtualRealitySDKs is obsolete, CS0618 => 'member' is obsolete: 'text'
namespace Razgriz
{
    [InitializeOnLoad]
    class SetVRPlayerSettings
    {
        const string TOOLBAR_PATH = "Tools/VR Player Presets/";
        const string SDK_NAME_MOCKHMD = "MockHMD";
        const string SDK_NAME_OPENVR = "OpenVR";
        const string SDK_NAME_OCULUS = "Oculus";
        const string SDK_NAME_NONE = "None";

        static string[] vrSdkList = new string[] { };
        static StereoRenderingPath stereoRenderingPath = StereoRenderingPath.Instancing;

        static SetVRPlayerSettings()
        {
            GetEditorPrefs();
            SetEditorPrefs();
            ApplySettings();

            EditorApplication.update -= ApplySettings;
            EditorApplication.update += ApplySettings;
#if VRC_SDK_VRCSDK3
            // harmonyInstance.PatchAll();
#endif
        }

#if VRC_SDK_VRCSDK3
/*
        static Harmony harmonyInstance = new Harmony("Razgriz.FixVRCSDKVRSettings");
        [HarmonyPatch(typeof(VRC.Editor.EnvConfig), nameof(VRC.Editor.EnvConfig.SetVRSDKs))]
        class PatchVRCSDKVRSettings
        {
            // Run after SetVRSDKs, ignore whatever happened, and just apply our preferred VR settings
            [HarmonyPostfix]
            static void Postfix(object __instance)
            {
                ApplySettings();
            }
        }
*/
#endif

        static void GetEditorPrefs()
        {
            if (EditorPrefs.HasKey("Razgriz.VRPlayerSettings"))
                vrSdkList = EditorPrefs.GetString("Razgriz.VRPlayerSettings").Split(',');

            if (!EditorPrefs.HasKey("Razgriz.VRPlayerSettings") || vrSdkList.Length < 2)
            {
                vrSdkList = PlayerSettings.GetAvailableVirtualRealitySDKs(EditorUserBuildSettings.selectedBuildTargetGroup);
                SetNone();
            }
        }

        static void SetEditorPrefs()
        {
            EditorPrefs.SetString("Razgriz.VRPlayerSettings", String.Join(",", vrSdkList));
        }

        public static void ApplySettings()
        {
            ApplySettings(vrSdkList, stereoRenderingPath);
        }
        public static void ApplySettings(string[] sdkList, StereoRenderingPath renderingPath)
        {
            if (!EditorApplication.isPlaying)
            {
                // Check if it changed before we actually set things
                if(sdkList[0] != PlayerSettings.GetVirtualRealitySDKs(EditorUserBuildSettings.selectedBuildTargetGroup)[0] || renderingPath != PlayerSettings.stereoRenderingPath)
                {
                    PlayerSettings.SetVirtualRealitySDKs(EditorUserBuildSettings.selectedBuildTargetGroup, sdkList);
                    PlayerSettings.stereoRenderingPath = renderingPath;
                    SetEditorPrefs();
                }
            }
        }

        static bool CanChangeSettings()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isUpdating;
        }

        static void SetSDK(string vrSDKName)
        {
            int index = Array.FindIndex(vrSdkList, w => w.Equals(vrSDKName));
            if (index >= 0)
            {
                vrSdkList[index] = vrSdkList[0];
                vrSdkList[0] = vrSDKName;
            }
            ApplySettings();
        }

        [MenuItem(TOOLBAR_PATH + "Mock HMD", true, 0)]
        static bool CanSetMockHMD()
        {
            return CanChangeSettings();
        }
        [MenuItem(TOOLBAR_PATH + "Mock HMD", false, 0)]
        public static void SetMockHMD()
        {
            if (!vrSdkList.Contains(SDK_NAME_MOCKHMD))
            {
                vrSdkList.Prepend(SDK_NAME_MOCKHMD);
                ApplySettings();
            }
            else
            {
                SetSDK(SDK_NAME_MOCKHMD);
            }
        }

        [MenuItem(TOOLBAR_PATH + "OpenVR", true, 0)]
        static bool CanSetOpenVR()
        {
            return CanChangeSettings() && vrSdkList.Contains(SDK_NAME_OPENVR);
        }
        [MenuItem(TOOLBAR_PATH + "OpenVR", false, 0)]
        public static void SetOpenVR()
        {
            SetSDK(SDK_NAME_OPENVR);
        }

        [MenuItem(TOOLBAR_PATH + "Oculus", true, 0)]
        static bool CanSetOculus()
        {
            return CanChangeSettings() && vrSdkList.Contains(SDK_NAME_OCULUS);
        }
        [MenuItem(TOOLBAR_PATH + "Oculus", false, 0)]
        public static void SetOculus()
        {
            SetSDK(SDK_NAME_OCULUS);
        }

        [MenuItem(TOOLBAR_PATH + "None", true, 0)]
        static bool CanSetNone()
        {
            return CanChangeSettings();
        }
        [MenuItem(TOOLBAR_PATH + "None", false, 0)]
        public static void SetNone()
        {
            SetSDK(SDK_NAME_NONE);
        }

        [MenuItem(TOOLBAR_PATH + "Stereo Instanced", true, 11)]
        static bool CanSetSinglePassInstanced()
        {
            return CanChangeSettings();
        }
        [MenuItem(TOOLBAR_PATH + "Stereo Instanced", false, 11)]
        public static void SetSinglePassInstanced()
        {
            stereoRenderingPath = StereoRenderingPath.Instancing;
            ApplySettings();
        }

        [MenuItem(TOOLBAR_PATH + "Stereo", true, 11)]
        static bool CanSetSinglePass()
        {
            return CanChangeSettings();
        }
        [MenuItem(TOOLBAR_PATH + "Stereo", false, 11)]
        public static void SetSinglePass()
        {
            stereoRenderingPath = StereoRenderingPath.SinglePass;
            ApplySettings();
        }

        [MenuItem(TOOLBAR_PATH + "Multipass", true, 11)]
        static bool CanSetMultipass()
        {
            return CanChangeSettings();
        }
        [MenuItem(TOOLBAR_PATH + "Multipass", false, 11)]
        public static void SetMultipass()
        {
            stereoRenderingPath = StereoRenderingPath.MultiPass;
            ApplySettings();
        }
    }
}
#pragma warning restore 0618
#endif