#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EnableTexturesReadWrite
{
    [MenuItem("Tools/Enable Read-Write nas Cartas uVegas")]
    static void Enable()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D", new[] { "Assets/uVegas/Images/Cards" });

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"[ReadWrite] {count} texturas atualizadas com sucesso!");
    }
}
#endif