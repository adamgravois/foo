// code adapted from https://medium.com/codex/hackn-slash-interlude-1-automating-your-unity-imports-cd2ae594bf5c

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class ClubAssetImporter : AssetPostprocessor
{
    private static readonly List<string> _CLUB_TEXTURE_TYPES = new List<string> {"-color", "-normal", "-metalness", "-roughness", "-ambientOcclusion"};
    
    private static bool _ShouldProcessModel(string assetPath)
    {
        // only process the files in: "Imports/<_PROCESSOR_FOLDER>"
        //if (!assetPath.Contains(Path.Combine("Imports", _PROCESSOR_FOLDER)))
        //    return false;
        // Debug.Log("should model?>" + assetPath);
        // only process FBX files
        if (!assetPath.EndsWith(".fbx"))
        {   
            // Debug.Log("asset is not fbx");
            return false;
        }
        Debug.Log("asset is fbx");
        return true;
    }

    private static (string, string, string) _ParseAssetPath(string assetPath)
    {
        string shapeName;
        // return enclosing folder, asset itself, and shape (no extention or modifier)
        string assetFolder = Path.GetDirectoryName(assetPath);
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        int position = assetName.LastIndexOf("-");
        if (position == -1)
        {
            shapeName = assetName;
        }
        else
        {
            shapeName = assetName.Substring(0, position);
        }
        Debug.Log($"   ParseAssetPath returning > {assetFolder}, {assetName}, {shapeName}");
        return (assetFolder, assetName, shapeName);
    }
 

    void OnPreprocessModel()
    {
        Debug.Log("Doing OnPreProcessModel> " + assetPath);  // ok this works
        
        string groupName, assetName, shapeName;
        (groupName, assetName, shapeName) = _ParseAssetPath(assetPath);
        
        string materialName = $"{shapeName}.mat";
        string materialPath = Path.Combine(groupName, materialName);
        // Check if material exists
        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            materialPath );   
        
        if (material != null)
        {
            Debug.Log($" material '{materialName}' already exists, skipping");
        }
        else
        {
            Debug.Log($" duplicating material '{materialName}' from template material");
            // the simple duplicate asset appraoch. its slow but it works
            const string aiMaterialPath = "Assets/Shaders/autodeskInteractive-mtl.mat";
            AssetDatabase.CopyAsset(aiMaterialPath, materialPath);
            // can we force the material onto the model? 

        }
    }

    void OnPostprocessModel(GameObject g)
        {
            // Apply(g.transform);
            Debug.LogError(g.GetComponent<Renderer>());

        }

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {


        string groupName, assetName, shapeName;
        foreach (string path in importedAssets)
        {
            Debug.Log($"  OPPAA path > {path}");
            // materialRefFolder = _ParseAssetPath(path);
            // materialAssetDir = _ParseAssetPath(path);// Path.Combine(materialsRootPath, materialRefFolder);
            (groupName, assetName, shapeName) = _ParseAssetPath(path);
             
            if (_ShouldProcessModel(path))
            {
                Debug.Log("approved to process model on path > " + path);
        

                string materialName = shapeName + ".mat" ;         
                // if material doesnt exist yet, create it
                // the below doesnt work; cant convert string to unityengine object
                //if (!AssetDatabase.Contains(materialName))
                //{
                //    Debug.Log($" material named {materialName} doesnt exist");
                //}
                // apply material to model

                
            }
            else if (_IsTexture(path) ) 
            {
                Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
                if (tex == null)
                {
                    Debug.LogWarning($"Could not find texture '{path}'" 
                        + "- no auto-linking of hte texture");
                    return;
                }
                Debug.Log($"  OOPPAA found texture '{path}'");

                string mapType = _ParseTextureType(path);
                //Debug.Log($" ooppaa material > {materialName} and map type {mapType}");

                TextureImporter tImporter = AssetImporter.GetAtPath( path ) as TextureImporter;
                if( tImporter == null ) {
                    Debug.Log($"  tImporter Failed for '{path}'");
                    continue;
                    //tImporter.mipmapEnabled = ...;
                    //tImporter.isReadable = ...;
                    //tImporter.maxTextureSize = ...;
                    //AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );                
                }

                if (mapType == "-color")
                {
                    Debug.Log($" doing COLOR actions");
                    tImporter.maxTextureSize = 4096;
                }
                else if (mapType == "-normal")
                {
                    Debug.Log($" doing NORMAL actions");
                    tImporter.normalmap = true;
                    tImporter.ignorePngGamma = true;
                    tImporter.maxTextureSize = 4096;
                    //tImporter.textureCompression = TextureImporterCompression.Uncompressed;

                }
                else if (mapType == "-metalness")
                {
                    Debug.Log($" doing METAL actions");
                    tImporter.sRGBTexture = false;
                    tImporter.ignorePngGamma = true;
                    tImporter.maxTextureSize = 4096;
                }
                else if (mapType == "-roughness")
                {
                    Debug.Log($" doing ROUGH actions");
                    tImporter.sRGBTexture = false;
                    tImporter.ignorePngGamma = true;
                    tImporter.maxTextureSize = 4096;
                }
                else if (mapType == "-ambientOcclusion")
                {
                    Debug.Log($" doing OCCLUDED actions");
                    tImporter.sRGBTexture = false;
                    tImporter.ignorePngGamma = true;
                    tImporter.maxTextureSize = 4096;
                }

                    
            }
            else if (_IsMaterial(path) )
            {
                // because it takes so fucking long to replicate the material
                // specified in preprocess,... all the material operations
                // have to wait until the material is built
                string materialName = $"{shapeName}.mat";
                string materialPath = Path.Combine(groupName, materialName);
                
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    path );     //Path.Combine(groupName, materialName)
                
                if (material == null)
                {
                    Debug.LogWarning($"Could not find material '{materialName}'"
                        + "- no auto linking of the textures");
                    continue;
                }
                
                Debug.Log($"  OPPAA found MATERIAL > {materialName}");

                // connect material to object
                // get object name
                string modelName = $"{shapeName}.fbx";
                string modelPath = Path.Combine(groupName, modelName);
                
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                // get object renderer??
                //Renderer model = AssetDatabase.LoadAssetAtPath<Renderer>(modelPath);
                // did it work?
                if (model == null)
                {
                    Debug.LogWarning($" OOPPAA couldnot get model '{modelName}'");
                }
                else
                {
                    // assign material to FBX
                    Debug.LogWarning($" OO got game object {model} yay");
                    model.GetComponent<MeshRenderer>().material = material;
                    //Debug.Log($"model material is {model.Material}");
                }
                // connect textures to material
                foreach (string mapType in _CLUB_TEXTURE_TYPES)
                {
                    // get textures for material and connect  them
                    // for each in CLUB TEXTURE TYPES
                    // get the texture path
                    string textureName = (shapeName + mapType + ".png");
                    string texturePath = Path.Combine(groupName, textureName );
                    // confirm it exists
                    Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                    if (tex == null)
                    {
                        Debug.LogWarning($"Could not find texture '{textureName}'" 
                            + "- no auto-linking of hte texture");
                        continue;
                    }
                    Debug.Log($"  MTL OOPPAA found texture '{textureName}'");
                        // based on name, hook up thusly

                    if (mapType == "-color")
                    {
                        Debug.Log($" doing COLOR actions");
                        material.SetTexture("_MainTex", tex);
                    }
                    else if (mapType == "-normal")
                    {
                        Debug.Log($" doing NORMAL actions");
                        material.SetTexture("_BumpMap", tex);
                    }
                    else if (mapType == "-metalness")
                    {
                        Debug.Log($" doing METAL actions");
                        material.SetTexture("_MetallicGlossMap", tex);
                    }
                    else if (mapType == "-roughness")
                    {
                        Debug.Log($" doing ROUGH actions");
                        material.SetTexture("_SpecGlossMap", tex);
                    }
                    else if (mapType == "-ambientOcclusion")
                    {
                        Debug.Log($" doing OCCLUDED actions");
                        
                        material.SetTexture("_OcclusionMap", tex);
                    }
                }    
            }
        }

    }

    private static bool _IsMaterial(string assetPath)
    {
        // kinda dumb, just ask it what its name is
        string p = assetPath.ToLower();
        return p.EndsWith(".mat");
    }

    private static bool _IsTexture(string assetPath)
    {
        string p = assetPath.ToLower();
        // Debug.Log("is texture? path>"+ assetPath);
        return p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".jpeg") || p.EndsWith(".tga");
    }

/*    private static bool _MaterialExists(string materialPath)
    {
        // this is a silly solution that Im using to force texture ops to 
        // wait until material is created (by the preprocessModel no less!!)
        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            materialPath );    
        if (material == null)
            return false;
        else 
            return true;
        
    } */

    private static string _ParseTextureType(string texPath)
    {
        (string dir, string assetName, string shapeName) = _ParseAssetPath(texPath);
        
        // club texture types include -color, -normal, etc
        foreach (string type in _CLUB_TEXTURE_TYPES)
        {
            // Debug.Log($"checking texture path for {type}");
            if (assetName.Contains(type))
            {
                Debug.Log($" ParseTextureName > {assetName} is type > {type}") ;
                return (type);
            }
        }
        Debug.Log($"   ParseTextureName did NOT find type for> {assetName}");
        return ("Unknown");
    }
}
