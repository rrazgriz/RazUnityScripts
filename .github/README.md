# Raz's Unity Scripts

Unity editor/tooling scripts that are useful to me that don't fit in their own full releases.

They're not polished and can break things. Use at your own risk and all that.

## Usage

To download all scripts:
- Clone or [Download](https://github.com/rrazgriz/RazUnityScripts/archive/refs/heads/main.zip) the repo and place the folder in your Unity project's Assets folder. Usage notes are included at the top of most scripts.

To use individual scripts:
- If it's not in an `Editor` folder: download the script and put it anywhere in your project's Assets folder.
- If it's in an `Editor` folder: needs to be in a subfolder of a folder named `Editor`, but that folder can be anywhere.

## Scripts

| Script  | Description | License |
| ------------- | ------------- | ------------- | 
| [FramerateLimiter.cs](../FramerateLimiter.cs) | Limit framerate in Play Mode. Add to a GameObject in the scene. | N/A |
| [SwapGlobalTexture.cs](../SwapGlobalTexture.cs) | Change a global texture property between two selected textures (or null). Add to a GameObject in the scene. | MIT |
| [YtdlpPlayer.cs](../YtdlpPlayer.cs) | Ytdlp -> Unity Video Player component. Meant for AudioLink testing. Add to a GameObject and point it at a video player. | MIT |
| [CopyGuidToClipboard.cs](../Editor/CopyGuidToClipboard.cs) | Right click assets -> `Copy GUID(s) to Clipboard`. Annotates selections of >1 object. | MIT |
| [PlayModeFocusHandler.cs](../Editor/PlayModeFocusHandler.cs) | Disable (or Enable) Play Mode forcing Game Mode to focused. `Tools -> Playmode Game View Focus`, fix applied by default. | MIT |
| [SetVRPlayerSettings.cs](../Editor/SetVRPlayerSettings.cs) | Quickly Set VR Player Settings in Unity 2019 (and keep them). `Tools -> VR Player Presets` | MIT |
| [UnityGuidRegenerator.cs](../Editor/UnityGuidRegenerator.cs) | Change GUID of assets in-place. Danger mode! Right click assets -> `Regenerate GUIDs` | N/A |
