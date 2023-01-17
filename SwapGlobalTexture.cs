// SPDX-License-Identifier: MIT
// Copyright 2023 Razgriz

// Tool to set a global texture property to one of two textures. Useful for testing shader behavior with AudioLink present/not present.
// Usage: Add to a GameObject in-scene. Use checkboxes to switch.

using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class SwapGlobalTexture : MonoBehaviour
{
    public Texture textureA, textureB;
    public bool swapToA, swapToB;
    public string texturePropertyToSet = "_AudioTexture";

    void Start()
    {
        #if UNITY_EDITOR
        if(textureA == null)
        {
            string[] findAudioLink = AssetDatabase.FindAssets("rt_audioLink t:rendertexture");
            if(findAudioLink.Length == 1)
                textureA = AssetDatabase.LoadAssetAtPath<RenderTexture>(AssetDatabase.GUIDToAssetPath(findAudioLink[0]));
        }
        #endif
    }

    void Update()
    {
        #if UNITY_EDITOR
        if(swapToA || swapToB)
        {
            if(swapToA)
                Shader.SetGlobalTexture(Shader.PropertyToID(texturePropertyToSet), textureA);
            else if(swapToB)
                Shader.SetGlobalTexture(Shader.PropertyToID(texturePropertyToSet), textureB);
            
            swapToA = swapToB = false;
        }
        #endif
    }
}
