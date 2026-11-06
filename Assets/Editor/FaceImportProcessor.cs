using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Makes the character picture folders "just work":
///
///  * sets sane import settings for anything dropped in the picture folders
///  * copies every PNG/JPG/JPEG from those folders into
///    Assets/StreamingAssets/CharacterImages/{Survivors|Witches}/ before a
///    build, so the standalone player reads the SAME files the editor does.
///
/// The runtime side (CharacterImageLibrary) reads that folder, so adding a
/// picture never needs a code change.
/// </summary>
public class FaceImportProcessor : AssetPostprocessor, IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    /// <summary>Folders a picture may be dropped into (any depth inside them).</summary>
    private static readonly string[] DropRoots =
    {
        "Assets/Characters/Faces/Survivors",
        "Assets/Characters/Faces/Witches",
        "Assets/Characters/Survivors",
        "Assets/Characters/Witches",
        "Assets/Faces/Survivors",
        "Assets/Faces/Witches",
    };

    /// <summary>Documented location: Assets/Resources/CharacterImages/{Survivors|Witches}/.</summary>
    private const string ImagesResourcesRoot = "Assets/Resources/CharacterImages";

    /// <summary>
    /// THE folders a build copies its pictures from. Only the official folder is
    /// staged, so an old copy of a character sitting in a legacy drop folder can
    /// never be shipped and shadow the real photo.
    /// </summary>
    private static readonly string[] StageRoots =
    {
        ImagesResourcesRoot + "/Survivors",
        ImagesResourcesRoot + "/Witches",
    };

    /// <summary>Legacy placeholder folder, still supported at lower priority.</summary>
    private const string LibraryFacesRoot = "Assets/Resources/CharacterFaces";

    private const string StreamingRoot = "Assets/StreamingAssets/CharacterImages";

    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

    private static bool _copying;

    // ---- import settings ---------------------------------------------------

    private void OnPreprocessTexture()
    {
        string normalized = assetPath.Replace('\\', '/');

        bool isDropFile = false;
        for (int i = 0; i < DropRoots.Length; i++)
        {
            if (normalized.StartsWith(DropRoots[i] + "/")) { isDropFile = true; break; }
        }

        bool isImagesResource = normalized.StartsWith(ImagesResourcesRoot + "/");
        bool isLegacyResource = normalized.StartsWith(LibraryFacesRoot + "/");

        if (!isDropFile && !isImagesResource && !isLegacyResource) return;

        var importer = (TextureImporter)assetImporter;

        // Must be a plain Texture: Resources.Load<Texture2D> returns null for
        // anything imported as a Sprite, which is the classic "my image never
        // shows up" trap.
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.isReadable = false;

        if (isLegacyResource)
        {
            // Left exactly as the existing placeholders were imported.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
        }
        else
        {
            // HD 620 friendly: at most 1024 px, block compressed.
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 1024;
        }
    }

    private void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (_copying) return;

        foreach (var p in imported)
        {
            string n = p.Replace('\\', '/');
            if (IsStagedPath(n) || IsDropPath(n))
            {
                if (IsImage(n))
                {
                    CopyPicturesToStreaming();
                    return;
                }
            }
        }
    }

    // ---- build step --------------------------------------------------------

    public void OnPreprocessBuild(BuildReport report)
    {
        CopyPicturesToStreaming();
    }

    private static bool IsStagedPath(string assetPath)
    {
        for (int i = 0; i < StageRoots.Length; i++)
            if (assetPath.StartsWith(StageRoots[i] + "/")) return true;
        return false;
    }

    private static bool IsDropPath(string assetPath)
    {
        for (int i = 0; i < DropRoots.Length; i++)
            if (assetPath.StartsWith(DropRoots[i] + "/")) return true;
        return false;
    }

    /// <summary>
    /// Rebuilds Assets/StreamingAssets/CharacterImages from THE OFFICIAL folder.
    /// The folder is generated, so it is cleared first - a deleted picture never
    /// lingers in the build and an updated one never loses to an old copy.
    /// </summary>
    private static void CopyPicturesToStreaming()
    {
        _copying = true;
        try
        {
            if (Directory.Exists(StreamingRoot)) Directory.Delete(StreamingRoot, true);

            int copied = 0;
            for (int i = 0; i < StageRoots.Length; i++)
            {
                string src = StageRoots[i];
                if (!Directory.Exists(src)) continue;

                string faction = src.Substring(src.LastIndexOf('/') + 1);
                string dst = Path.Combine(StreamingRoot, faction);
                Directory.CreateDirectory(dst);

                foreach (var file in Directory.GetFiles(src))
                {
                    if (!IsImage(file)) continue;
                    try
                    {
                        File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
                        copied++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[CHARACTER IMAGE] copy failed for " + file + ": " + e.Message);
                    }
                }
            }

            if (copied > 0) Debug.Log("[CHARACTER IMAGE] staged " + copied + " picture(s) into " + StreamingRoot);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CHARACTER IMAGE] staging failed: " + e.Message);
        }
        finally
        {
            _copying = false;
            AssetDatabase.Refresh();
        }
    }

    private static bool IsImage(string path)
    {
        string ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return false;
        ext = ext.ToLowerInvariant();
        for (int i = 0; i < Extensions.Length; i++)
            if (ext == Extensions[i]) return true;
        return false;
    }
}
