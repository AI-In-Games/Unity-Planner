using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIInGames.Planning.Demo.EditorTools
{
    /// <summary>
    /// Shared placeholder art for the demos. Provides a white square sprite, created on disk and
    /// forced to import as a Sprite, so the scene builders always have a non-null sprite to assign.
    /// </summary>
    public static class DemoPlaceholders
    {
        public const string WhiteSquarePath = "Assets/Sprites/WhiteSquare.png";

        public static Sprite WhiteSquare()
        {
            if (!File.Exists(WhiteSquarePath))
            {
                Texture2D texture = new Texture2D(8, 8);
                Color32[] pixels = new Color32[64];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(WhiteSquarePath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(WhiteSquarePath, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(WhiteSquarePath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(WhiteSquarePath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(WhiteSquarePath) as TextureImporter;
            }

            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (importer.spritePixelsPerUnit != 8f)
                {
                    importer.spritePixelsPerUnit = 8f;
                    changed = true;
                }
                if (changed)
                    importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);
            if (sprite == null)
                Debug.LogError($"[Demo] Could not load a Sprite at {WhiteSquarePath}. Check its import settings.");
            return sprite;
        }
    }
}
