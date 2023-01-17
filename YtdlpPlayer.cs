using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

// This component uses code from the following sources:
// UnityYoutubePlayer, courtesy iBicha (SPDX-License-Identifier: Unlicense) https://github.com/iBicha/UnityYoutubePlayer
// USharpVideo, Copyright (c) 2020 Merlin, (SPDX-License-Identifier: MIT) https://github.com/MerlinVR/USharpVideo/

#if UNITY_EDITOR
namespace Raz
{
    /// <summary> Downloads and plays videos via a VideoPlayer component </summary>
    public class YtdlpPlayer : MonoBehaviour
    {
        /// <summary> Ytdlp url (e.g. https://www.youtube.com/watch?v=SFTcZ1GXOCQ) </summary>
        public string ytdlpURL = "https://www.youtube.com/watch?v=SFTcZ1GXOCQ";

        /// <summary> VideoPlayer component associated with the current YtdlpPlayer instance </summary>
        [SerializeReference]
        public VideoPlayer videoPlayer = null;

        [SerializeReference]
        public AudioSource audioSourceOutput = null;

        /// <summary> Initialize and play from URL </summary>
        void OnEnable()
        {
            if(videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();

            PrepareVideoPlayer();
            UpdateAndPlay();
        }

        /// <summary> Set up VideoPlayer component </summary>
        public void PrepareVideoPlayer()
        {
            if(videoPlayer != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.source = VideoSource.Url;
                videoPlayer.isLooping = true;
                videoPlayer.playOnAwake = true;

                if(audioSourceOutput != null)
                    videoPlayer.SetTargetAudioSource(0, audioSourceOutput);
            }
        }

        public void SetAudioSourceVolume(float volume)
        {
            if(audioSourceOutput != null)
                audioSourceOutput.volume = Mathf.Clamp01(volume);
        }

        public bool GetAudioSourceVolume(out float volume)
        {
            if(audioSourceOutput == null)
            {
                volume = 0;
                return false;
            }
            
            volume = audioSourceOutput.volume;
            return true;
        }

        /// <summary> Update URL and start playing </summary>
        public void UpdateAndPlay()
        {
            UpdateURL();
            if (videoPlayer != null && videoPlayer.length > 0)
                videoPlayer.Play();
        }

        /// <summary> Set time to zero, resolve, and set URL </summary>
        public void UpdateURL()
        {
            string resolved = YtdlpURLResolver.Resolve(ytdlpURL, 720);
            if(videoPlayer != null && resolved != null)
            {
                videoPlayer.url = resolved;
                SetPlaybackTime(0.0f);
            }
        }

        /// <summary> Get Video Player Playback Time (as a fraction of playback, 0-1) </summary>
        public float GetPlaybackTime()
        {
            if(videoPlayer != null && videoPlayer.length > 0)
                return (float)(videoPlayer.length > 0 ? videoPlayer.time / videoPlayer.length : 0);
            else
                return 0;
        }

        /// <summary> Set Video Player Playback Time (Seek) </summary>
        /// <param name="time">Fraction of playback (0-1) to seek to</param>
        public void SetPlaybackTime(float time)
        {
            if(videoPlayer != null && videoPlayer.length > 0 && videoPlayer.canSetTime)
                videoPlayer.time = videoPlayer.length * Mathf.Clamp(time, 0.0f, 1.0f);
        }

        /// <summary> Format seconds as hh:mm:ss or mm:ss </summary>
        public string FormattedTimestamp(double seconds, double maxSeconds=0)
        {
            double formatValue = maxSeconds > 0 ? maxSeconds : seconds;
            string formatString = formatValue >= 3600.0 ? @"hh\:mm\:ss" : @"mm\:ss";
            return TimeSpan.FromSeconds(seconds).ToString(formatString);
        }

        /// <summary> Get Video Player Playback Time formatted as current / length </summary>
        public string CurrentTimeFormatted()
        {
            if(videoPlayer != null && videoPlayer.length > 0)
            {
                return FormattedTimestamp(videoPlayer.time, videoPlayer.length);
            }
            else
                return "00:00";
        }

        /// <summary> Get Video Player Playback Time formatted as current / length </summary>
        public string TotalTimeFormatted()
        {
            if(videoPlayer != null && videoPlayer.length > 0)
            {
                return FormattedTimestamp(videoPlayer.length);
            }
            else
                return "00:00";
        }
    }

    [CustomEditor(typeof(YtdlpPlayer))]
    public class YtdlpPlayerEditor : UnityEditor.Editor 
    {
        YtdlpPlayer _ytdlpPlayer;
        bool componentGroupFoldout = false;

        void OnEnable()
        {
            _ytdlpPlayer = (YtdlpPlayer) target;
        }

        // Force constant updates when playing, so playback time is not behind
        public override bool RequiresConstantRepaint()
        {
            if(_ytdlpPlayer.videoPlayer != null)
                return _ytdlpPlayer.videoPlayer.isPlaying;
            else
                return false;
        }

        public override void OnInspectorGUI()
        {
            #if UNITY_EDITOR_LINUX
                EditorGUILayout.HelpBox("The Unity VideoPlayer component only supports the following formats on Linux: .ogv, .vp8, .webm", MessageType.Warning);
            #endif
            float playbackTime = 0;

            bool hasPlayer = _ytdlpPlayer.videoPlayer != null;
            if(hasPlayer && _ytdlpPlayer.videoPlayer.length > 0)
                playbackTime = _ytdlpPlayer.GetPlaybackTime();

            // URL Input
            _ytdlpPlayer.ytdlpURL = EditorGUILayout.TextField("URL", _ytdlpPlayer.ytdlpURL);

            if(!Application.IsPlaying(target))
                EditorGUILayout.HelpBox("Enter Play Mode to use this component!", MessageType.Info);

            // Timestamp/Reload button
            using (new EditorGUI.DisabledScope(!hasPlayer || !Application.IsPlaying(target)))
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent($" Seek: {_ytdlpPlayer.CurrentTimeFormatted()} / {_ytdlpPlayer.TotalTimeFormatted()}", EditorGUIUtility.IconContent("d_Slider Icon").image));
                bool updateURL = GUILayout.Button(new GUIContent(" Reload", EditorGUIUtility.IconContent("TreeEditor.Refresh").image));
                if(updateURL)
                    _ytdlpPlayer.UpdateAndPlay();
            }

            // Seek position should not be editable if video is not playing
            using (new EditorGUI.DisabledScope(!hasPlayer || !Application.IsPlaying(target) || !_ytdlpPlayer.videoPlayer.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                // Seekbar input
                EditorGUI.BeginChangeCheck();
                playbackTime = GUILayout.HorizontalSlider(playbackTime, 0, 1);
                if(EditorGUI.EndChangeCheck())
                    _ytdlpPlayer.SetPlaybackTime(playbackTime);

                // Timestamp input
                EditorGUI.BeginChangeCheck();
                string currentTimestamp = " " + _ytdlpPlayer.CurrentTimeFormatted();
                string seekTimestamp = EditorGUILayout.DelayedTextField(currentTimestamp, GUILayout.MaxWidth(8 * currentTimestamp.Length));
                if(EditorGUI.EndChangeCheck())
                {
                    TimeSpan inputTimestamp;
                    // Add an extra 00: to force TimeSpan to interpret 12:34 as 00:12:34 for proper mm:ss input
                    if(TimeSpan.TryParse($"00:{seekTimestamp}", out inputTimestamp))
                    {
                        playbackTime = (float)(inputTimestamp.TotalSeconds / _ytdlpPlayer.videoPlayer.length);
                        _ytdlpPlayer.SetPlaybackTime(playbackTime);
                    }
                }
            }

            float volume;
            using (new EditorGUI.DisabledScope(!_ytdlpPlayer.GetAudioSourceVolume(out volume)))
            {
                EditorGUI.BeginChangeCheck();
                volume = EditorGUILayout.Slider(new GUIContent("  AudioSource Gain", EditorGUIUtility.IconContent("d_Profiler.Audio").image), volume, 0.0f, 1.0f);
                if(EditorGUI.EndChangeCheck())
                    _ytdlpPlayer.SetAudioSourceVolume(volume);
            }

            componentGroupFoldout = EditorGUILayout.Foldout(componentGroupFoldout, "Components");
            if(componentGroupFoldout)
            {
                // Video Player/Audio Source
                _ytdlpPlayer.videoPlayer = (VideoPlayer) EditorGUILayout.ObjectField(new GUIContent("  VideoPlayer", EditorGUIUtility.IconContent("d_Profiler.Video").image), _ytdlpPlayer.videoPlayer, typeof(VideoPlayer), allowSceneObjects: true);
                _ytdlpPlayer.audioSourceOutput = (AudioSource) EditorGUILayout.ObjectField(new GUIContent("  AudioSource", EditorGUIUtility.IconContent("d_Profiler.Audio").image), _ytdlpPlayer.audioSourceOutput, typeof(AudioSource), allowSceneObjects: true);
            }

            if(GUI.changed){ EditorUtility.SetDirty(_ytdlpPlayer); }

        }
    }

    [InitializeOnLoad]
    public static class YtdlpURLResolver
    {
        private static string _ytdlpDownloadURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        private static string _localYtdlpPath = Application.dataPath + "\\AudioLink\\yt-dlp.exe";

        private static string _ytdlpPath = "";
        private static bool _ytdlFound = false;

        /// <summary> Locate yt-dlp executible, either in VRC application data or locally (offer to download) </summary>
        public static void LocateYtdlp()
        {
            _ytdlFound = false;
            #if UNITY_EDITOR_WIN
            string[] splitPath = Application.persistentDataPath.Split('/', '\\');
            
            // Check for yt-dlp in VRC application data first
            _ytdlpPath = string.Join("\\", splitPath.Take(splitPath.Length - 2)) + "\\VRChat\\VRChat\\Tools\\yt-dlp.exe";
            #elif UNITY_EDITOR_LINUX
            _ytdlpPath = "/usr/bin/yt-dlp";
            #endif
            if (!File.Exists(_ytdlpPath)) 
            {
                // Check the local path (in the Assets folder)
                _ytdlpPath = _localYtdlpPath;
            }

            if (!File.Exists(_ytdlpPath))
            {
                #if UNITY_EDITOR_WIN
                // Offer to download yt-dlp to the AudioLink folder
                bool doDownload = EditorUtility.DisplayDialog("[AudioLink] Download yt-dlp?", "AudioLink could not locate yt-dlp in your VRChat folder.\nDownload to AudioLink folder instead?", "Download", "Cancel");
                if(doDownload)
                    DownloadYtdlp();

                if(!Application.isPlaying)
                    EditorApplication.ExitPlaymode();
                
                #elif UNITY_EDITOR_LINUX
                    EditorUtility.DisplayDialog("[AudioLink] Missing yt-dlp", "Ensure yt-dlp is available in your PATH", "Ok");
                #endif
            }

            if (!File.Exists(_ytdlpPath)) 
            {
                // Still don't have it, no dice
                Debug.LogWarning("[AudioLink] Unable to find yt-dlp");
                return;
            }
            else
            {
                // Found it
                _ytdlFound = true;
                Debug.Log($"[AudioLink] Found yt-dlp at path '{_ytdlpPath}'");
            }
        }

        /// <summary> Resolves a URL to one usable in a VideoPlayer. </summary>
        /// <param name="url">URL to resolve for playback</param>
        /// <param name="resolution">Resolution (vertical) to request from yt-dlp</param>
        public static string Resolve(string url, int resolution)
        {
            if(!_ytdlFound)
            {
                LocateYtdlp();
                if(!_ytdlFound)
                    return null;
            }

            using (var ytdlProcess = new Process())
            {
                ytdlProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ytdlProcess.StartInfo.CreateNoWindow = true;
                ytdlProcess.StartInfo.UseShellExecute = false;
                ytdlProcess.StartInfo.RedirectStandardOutput = true;
                ytdlProcess.StartInfo.FileName = _ytdlpPath;
                ytdlProcess.StartInfo.Arguments = $"--no-check-certificate --no-cache-dir --rm-cache-dir -f \"mp4[height<=?{resolution}]/best[height<=?{resolution}]\" --get-url \"{url}\"";

                try
                {
                    ytdlProcess.Start();

                    int waits = 0;
                    while (!ytdlProcess.HasExited)
                    {
                        if(waits > 50)
                            break;
                        waits++;
                        new WaitForSeconds(0.1f);
                    }

                    string output = ytdlProcess.StandardOutput.ReadLine();

                    return output;
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"[AudioLink] Unable to resolve URL '{url}' : " + e.Message);
                    return null;
                }

            }
        }

        /// <summary> Download yt-dlp to the AudioLink folder. </summary>
        private static void DownloadYtdlp()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(new Uri(_ytdlpDownloadURL), _localYtdlpPath);
                    Debug.Log($"[AudioLink] yt-dlp downloaded to '{_ytdlpPath}'");
                    AssetDatabase.Refresh();
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"[AudioLink] Failed to download yt-dlp from '{_ytdlpDownloadURL}' : " + e.Message);
                }

                // Check for it again to make sure it was actually downloaded
                LocateYtdlp();
            }
        }
    }
}
#endif