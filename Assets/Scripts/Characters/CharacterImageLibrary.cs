using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// One resolved character picture: the texture plus where it came from.
/// <c>isFallback</c> is true when the procedural face was generated because no
/// usable image file existed.
/// </summary>
public struct CharacterImage
{
    public Texture2D texture;
    public bool isFallback;
    public string source;
    public int width;
    public int height;

    public float Aspect { get { return height > 0 ? (float)width / height : 1f; } }
    public bool IsValid { get { return texture != null; } }
}

/// <summary>
/// THE character picture pipeline. Every portrait - the 3D face quad, the
/// Character Select card and the Character Library - goes through here, so an
/// image only has to exist once in one place to appear everywhere.
///
/// ==========================================================================
///  WHERE TO PUT A CHARACTER PICTURE  (no code change needed)
///
///  1. THE OFFICIAL LOCATION (recommended, works in builds):
///       Assets/Resources/CharacterImages/Survivors/&lt;official-file&gt;
///       Assets/Resources/CharacterImages/Witches/&lt;official-file&gt;
///     Official file names (lower case, hyphenated, .png/.jpg/.jpeg):
///       Survivors: luis-padilla, mick-richards, josh-garcia, vincent-reid
///       Witches:   rona-tanyel, demonisa-belle, kyla-ni-garcia, jannak,
///                  sandy-vill, beamoth
///
///  2. Drop folder (auto-copied into the build by FaceImportProcessor):
///       Assets/Characters/Faces/Survivors/&lt;name&gt;.png|jpg|jpeg
///       Assets/Characters/Faces/Witches/&lt;name&gt;.png|jpg|jpeg
///     (Assets/Characters/&lt;Survivors|Witches&gt;/ and Assets/Faces/... are also
///      scanned so art dropped in the older folders keeps working.)
///
///  3. No rebuild at all (great for testing):
///       &lt;persistentDataPath&gt;/CharacterImages/&lt;name&gt;.png|jpg|jpeg
///       Linux: ~/.config/unity3d/&lt;company&gt;/&lt;product&gt;/CharacterImages/
///     Sub-folders Survivors/ and Witches/ are honoured here too.
///
///  The file name is matched case-insensitively against the OFFICIAL file name
///  first, then the display name ("Luis Padilla"), the compact form
///  ("LuisPadilla"), the stable internal id ("ethan_vale") and the short/legacy
///  name ("Ethan", "Mara").  Previous display names are accepted too, so art
///  named after an older name keeps resolving after a rename.
///  .png, .jpg and .jpeg are all accepted, whatever the actual encoding inside
///  the file is, and the extension case does not matter.
/// ==========================================================================
///
/// Textures are decoded once, cached by character and shared by gameplay and
/// UI, so nothing is re-read per frame or per menu visit.
/// </summary>
public static class CharacterImageLibrary
{
    /// <summary>Longest edge a decoded picture is allowed to keep (HD 620 friendly).</summary>
    public const int MaxTextureSize = 1024;

    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

    private static readonly Dictionary<string, CharacterImage> _cache =
        new Dictionary<string, CharacterImage>(System.StringComparer.Ordinal);

    private static readonly Dictionary<string, List<string>> _dirCache =
        new Dictionary<string, List<string>>(System.StringComparer.Ordinal);

    /// <summary>Decoded file textures, keyed by full path: one decode per file.</summary>
    private static readonly Dictionary<string, Texture2D> _fileCache =
        new Dictionary<string, Texture2D>(System.StringComparer.Ordinal);

    /// <summary>Last write time of each decoded file, so a replaced picture is re-read.</summary>
    private static readonly Dictionary<string, long> _fileTimes =
        new Dictionary<string, long>(System.StringComparer.Ordinal);

    private static Dictionary<string, Texture2D> _resourceIndex;
    private static readonly HashSet<string> _logged = new HashSet<string>(System.StringComparer.Ordinal);
    private static readonly HashSet<string> _applied = new HashSet<string>(System.StringComparer.Ordinal);
    private static readonly List<Texture2D> _owned = new List<Texture2D>();

    // =====================================================================
    // public API
    // =====================================================================

    /// <summary>Resolve the picture for a character (cached). Never returns null texture.</summary>
    public static CharacterImage Get(string id, string displayName = null, WitchType witchType = WitchType.Humanoid)
    {
        string key = KeyFor(id, displayName);
        CharacterImage image;
        if (_cache.TryGetValue(key, out image) && image.IsValid) return image;

        image = Resolve(key, id, displayName, witchType);
        _cache[key] = image;
        return image;
    }

    /// <summary>Convenience for CharacterLore entries.</summary>
    public static CharacterImage GetFor(CharacterEntry entry)
    {
        return Get(entry.id, entry.name, entry.witchType);
    }

    /// <summary>Just the texture (still never null - falls back to the procedural face).</summary>
    public static Texture2D GetTexture(string id, string displayName = null, WitchType witchType = WitchType.Humanoid)
    {
        return Get(id, displayName, witchType).texture;
    }

    /// <summary>Resolve by display name only (used by the runtime factory).</summary>
    public static Texture2D GetTextureForName(string displayName, bool isWitch, WitchType witchType = WitchType.Humanoid)
    {
        int idx = CharacterLore.IndexOfName(displayName);
        string id = idx >= 0 ? CharacterLore.All[idx].id : null;
        WitchType type = isWitch ? witchType : WitchType.Humanoid;
        return Get(id, displayName, type).texture;
    }

    /// <summary>Lower-case, letters+digits only. "Rona Tanyel" and "rona_tanyel.png" both become "ronatanyel".</summary>
    public static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = char.ToLowerInvariant(value[i]);
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// "Cover" UV rect: fills the target box without stretching, cropping the
    /// overflow evenly. Used by RawImage.uvRect.
    /// </summary>
    public static Rect CoverUv(float imageAspect, float targetAspect)
    {
        if (imageAspect <= 0f || targetAspect <= 0f) return new Rect(0f, 0f, 1f, 1f);

        float sx = 1f, sy = 1f;
        if (imageAspect > targetAspect) sx = targetAspect / imageAspect;   // crop left/right
        else sy = imageAspect / targetAspect;                              // crop top/bottom

        return new Rect((1f - sx) * 0.5f, (1f - sy) * 0.5f, sx, sy);
    }

    /// <summary>
    /// Applies a cover-crop to a face material. The face quad is rotated 180°
    /// around Y so it points along the character's forward axis, which mirrors
    /// the texture horizontally - <paramref name="mirrorX"/> undoes that.
    /// </summary>
    public static void ApplyFaceUv(Material material, float imageAspect, float quadAspect, bool mirrorX)
    {
        if (material == null || !material.HasProperty("_MainTex")) return;

        float sx = 1f, sy = 1f;
        if (imageAspect > quadAspect) sx = quadAspect / imageAspect;
        else sy = imageAspect / quadAspect;

        float ox = (1f - sx) * 0.5f;
        float oy = (1f - sy) * 0.5f;

        if (mirrorX)
        {
            material.mainTextureScale = new Vector2(-sx, sy);
            material.mainTextureOffset = new Vector2(ox + sx, oy);
        }
        else
        {
            material.mainTextureScale = new Vector2(sx, sy);
            material.mainTextureOffset = new Vector2(ox, oy);
        }
    }

    /// <summary>
    /// Puts a picture on a UI RawImage, cover-cropped so it fills the frame
    /// without stretching (a 3:4 photo in a square card is cropped, not squashed).
    /// </summary>
    /// <summary>
    /// Puts a picture on a UI RawImage, cover-cropped so it fills the frame
    /// without stretching (a 3:4 photo in a square card is cropped, not squashed).
    /// When <paramref name="focusFace"/> is set the crop zooms toward the face and
    /// biases the framing upward, so a witch's head fills more of the frame and
    /// the empty space around the portrait shrinks.
    /// </summary>
    public static void ApplyCover(UnityEngine.UI.RawImage target, CharacterImage image, bool focusFace = false)
    {
        if (target == null || !image.IsValid) return;

        target.texture = image.texture;

        float frameAspect = 0f; // 0 -> CoverUv returns the full texture
        var rect = target.rectTransform.rect;
        if (rect.width > 1f && rect.height > 1f) frameAspect = rect.width / rect.height;

        target.uvRect = focusFace
            ? CoverUvFace(image.Aspect, frameAspect, 1.22f, 0.60f)
            : CoverUv(image.Aspect, frameAspect);
    }

    /// <summary>
    /// Like <see cref="CoverUv"/> but zoomed by <paramref name="zoom"/> and
    /// centred on <paramref name="pivotV"/> (texture V, 0 = bottom, 1 = top) so
    /// the face - which sits in the upper part of a head-and-shoulders photo -
    /// dominates the frame. Clamped so the window never leaves the texture.
    /// </summary>
    public static Rect CoverUvFace(float imageAspect, float targetAspect, float zoom, float pivotV)
    {
        if (imageAspect <= 0f || targetAspect <= 0f) return new Rect(0f, 0f, 1f, 1f);

        float sx = 1f, sy = 1f;
        if (imageAspect > targetAspect) sx = targetAspect / imageAspect;   // crop left/right
        else sy = imageAspect / targetAspect;                              // crop top/bottom

        float w = sx / zoom;
        float h = sy / zoom;

        float u = (1f - w) * 0.5f;            // centre horizontally
        float v = pivotV - h * 0.5f;          // put the face at the frame centre

        if (v < 0f) v = 0f;
        if (v + h > 1f) v = 1f - h;
        if (u < 0f) u = 0f;
        if (u + w > 1f) u = 1f - w;

        return new Rect(u, v, w, h);
    }

    /// <summary>Forgets cached folders/files (call after dropping in new art at runtime).</summary>
    public static void Refresh()
    {
        _cache.Clear();
        _dirCache.Clear();
        _resourceIndex = null;
        _fileCache.Clear();
        _fileTimes.Clear();
        _logged.Clear();
        _applied.Clear();
    }
    /// <summary>One log line per (context, character) so nothing spams the console.</summary>
    public static void LogApplied(string context, string key)
    {
        string tag = context + "|" + key;
        if (!_applied.Add(tag)) return;
        Debug.Log($"[CHARACTER IMAGE] Applied {context}: {key}");
    }

    /// <summary>Destroys every texture this library created and clears the cache.</summary>
    public static void ReleaseAll()
    {
        for (int i = 0; i < _owned.Count; i++)
            if (_owned[i] != null) Object.Destroy(_owned[i]);
        _owned.Clear();
        _cache.Clear();
        _resourceIndex = null;
        _dirCache.Clear();
        _fileCache.Clear();
        _fileTimes.Clear();
        _logged.Clear();
        _applied.Clear();
    }

    /// <summary>Diagnostic summary, logged once when the library first resolves art.</summary>
    public static string Describe()
    {
        int real = 0, fallback = 0;
        foreach (var kv in _cache)
        {
            if (kv.Value.isFallback) fallback++;
            else real++;
        }
        return $"cached={_cache.Count} images={real} fallbacks={fallback}";
    }

    // =====================================================================
    // resolution
    // =====================================================================

    /// <summary>
    /// Cache/lookup key. Always the stable internal id when the character is on
    /// the roster, so renaming a character's display name never changes which
    /// picture it resolves to.
    /// </summary>
    private static string KeyFor(string id, string displayName)
    {
        if (!string.IsNullOrEmpty(id)) return Normalize(id);

        if (!string.IsNullOrEmpty(displayName))
        {
            int idx = CharacterLore.IndexOfAny(displayName);
            if (idx >= 0) return Normalize(CharacterLore.All[idx].id);
            return Normalize(displayName);
        }

        return "unknown";
    }

    private static CharacterImage Resolve(string key, string id, string displayName, WitchType witchType)
    {
        string label = !string.IsNullOrEmpty(displayName) ? displayName
                     : (!string.IsNullOrEmpty(id) ? id : key);

        int idx = ResolveIndex(id, displayName);
        bool isWitch = idx >= 0 ? CharacterLore.IsMonsterAt(idx)
                                 : (witchType != WitchType.Humanoid || IsWitchKey(key));
        string faction = isWitch ? "Witches" : "Survivors";

        // Candidate file stems in priority order. The OFFICIAL file name always
        // wins; the older names stay valid so existing art never has to be
        // renamed. The first stem that resolves to a real file wins.
        var stems = BuildStemCandidates(key, id, displayName, idx);

        for (int i = 0; i < stems.Count; i++)
        {
            string stem = stems[i];

            // ---- 1. files on disk (runtime drop folder, shipped files, editor folders)
            string path = FindFileForStem(stem, faction);
            if (path != null)
            {
                string error;
                Texture2D tex = LoadFile(path, out error);
                if (tex != null)
                {
                    LogLoaded(label, tex, path);
                    return Make(tex, false, path);
                }
                LogInvalid(path, error);
            }

            // ---- 2. Unity Resources (imported textures, always present in builds)
            Texture2D res = FindResourceForStem(stem, faction);
            if (res != null)
            {
                LogLoaded(label, res, "Resources/" + faction);
                return Make(res, false, "Resources");
            }
        }

        // ---- 3. procedural fallback, still cached so it is generated once
        Texture2D fallback = isWitch
            ? ProceduralFaceGenerator.GenerateWitchFace(witchType)
            : ProceduralFaceGenerator.GenerateSurvivorFace(label);

        if (fallback != null) fallback.name = "CharacterImageFallback_" + key;
        LogMissing(label, key);
        return Make(fallback, true, "procedural");
    }

    /// <summary>Roster index for an id or a name (-1 when the character is unknown).</summary>
    private static int ResolveIndex(string id, string displayName)
    {
        if (!string.IsNullOrEmpty(id))
        {
            int i = CharacterLore.IndexOfAny(id);
            if (i >= 0) return i;
        }
        if (!string.IsNullOrEmpty(displayName))
        {
            int i = CharacterLore.IndexOfAny(displayName);
            if (i >= 0) return i;
        }
        return -1;
    }

    /// <summary>
    /// Every file stem this character may legitimately be saved as, in priority
    /// order:
    ///
    ///   1. the OFFICIAL file name ("luis-padilla")
    ///   2. the display name ("Luis Padilla", "LuisPadilla")
    ///   3. the stable internal id ("ethan_vale")
    ///   4. the short/legacy art name ("Ethan")
    ///   5. the previous display name ("Ethan Vale")
    ///   6. the first word of the current and previous name ("Luis", "Ethan")
    ///
    /// The official name is first, so a picture saved under the documented file
    /// name always wins over an older picture of the same character.
    /// </summary>
    private static List<string> BuildStemCandidates(string key, string id, string displayName, int idx)
    {
        var stems = new List<string>(8);
        var seen = new HashSet<string>(System.StringComparer.Ordinal);

        if (idx >= 0)
        {
            var entry = CharacterLore.All[idx];
            string legacy = CharacterLore.LegacyNameAt(idx);

            AddStem(stems, seen, CharacterLore.OfficialFileAt(idx));
            AddStem(stems, seen, entry.name);
            AddStem(stems, seen, entry.name.Replace(" ", ""));
            AddStem(stems, seen, entry.id);
            AddStem(stems, seen, CharacterFaceConfig.LegacyFileForId(entry.id));
            AddStem(stems, seen, legacy);
            if (!string.IsNullOrEmpty(legacy)) AddStem(stems, seen, legacy.Replace(" ", ""));
            AddStem(stems, seen, FirstWord(entry.name));
            AddStem(stems, seen, FirstWord(legacy));
        }

        AddStem(stems, seen, key);
        AddStem(stems, seen, id);
        AddStem(stems, seen, displayName);
        if (!string.IsNullOrEmpty(displayName))
        {
            AddStem(stems, seen, displayName.Replace(" ", ""));
            AddStem(stems, seen, CharacterFaceConfig.LegacyFileForName(displayName));
            AddStem(stems, seen, FirstWord(displayName));
        }

        return stems;
    }

    private static string FirstWord(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        int space = name.IndexOf(' ');
        return space > 0 ? name.Substring(0, space) : name;
    }

    private static void AddStem(List<string> stems, HashSet<string> seen, string raw)
    {
        string n = Normalize(raw);
        if (string.IsNullOrEmpty(n)) return;
        if (!seen.Add(n)) return;
        stems.Add(n);
    }


    private static CharacterImage Make(Texture2D tex, bool fallback, string source)
    {
        return new CharacterImage
        {
            texture = tex,
            isFallback = fallback,
            source = source,
            width = tex != null ? tex.width : 0,
            height = tex != null ? tex.height : 0
        };
    }

    /// <summary>True when the key belongs to a witch entry in CharacterLore.</summary>
    private static bool IsWitchKey(string key)
    {
        for (int i = 0; i < CharacterLore.Count; i++)
        {
            if (Normalize(CharacterLore.All[i].id) == key) return CharacterLore.All[i].isMonster;
            if (Normalize(CharacterLore.All[i].name) == key) return CharacterLore.All[i].isMonster;
        }
        return false;
    }

    private static string[] FileRoots()
    {
        var roots = new List<string>(6);

        // Runtime drop folder: add a picture and restart, no rebuild.
        AddRoot(roots, Path.Combine(Application.persistentDataPath, "CharacterImages"));

#if UNITY_EDITOR
        // Editor play mode: read THE OFFICIAL project folder directly, so a
        // replaced file is picked up immediately - no reimport, no play-mode
        // restart. This sits above the staged StreamingAssets copy so the
        // editor can never show an older staged picture.
        AddRoot(roots, Path.Combine(Application.dataPath, "Resources/CharacterImages"));
#endif

        // Shipped files (copied from the official folder by FaceImportProcessor
        // before a build). In a build these are the editable copies, so a
        // picture can be replaced there without rebuilding.
        AddRoot(roots, Path.Combine(Application.streamingAssetsPath, "CharacterImages"));
        AddRoot(roots, Path.Combine(Application.streamingAssetsPath, "Faces"));

#if UNITY_EDITOR
        // Legacy drop folders, lowest priority - kept so old art keeps working.
        AddRoot(roots, Path.Combine(Application.dataPath, "Characters/Faces"));
        AddRoot(roots, Path.Combine(Application.dataPath, "Characters"));
        AddRoot(roots, Path.Combine(Application.dataPath, "Faces"));
#endif
        return roots.ToArray();
    }

    private static void AddRoot(List<string> roots, string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (roots.Contains(path)) return;
        roots.Add(path);
    }

    // ---- file system -------------------------------------------------------

    /// <summary>
    /// Searches every file root for one exact stem. Roots are ordered by
    /// priority: the runtime drop folder first (so a replacement needs no
    /// rebuild), then the shipped/staged folders, then the editor drop folders.
    /// </summary>
    private static string FindFileForStem(string stem, string faction)
    {
        string[] roots = FileRoots();
        for (int i = 0; i < roots.Length; i++)
        {
            string hit = FindFileInRoot(roots[i], stem, faction);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>
    /// Finds the file for one exact stem inside a root. The character's own
    /// folder is searched first, then the folder itself, then the remaining
    /// sub-folders - but never the opposite faction's folder, so a witch
    /// picture can never be handed to a survivor.
    /// </summary>
    private static string FindFileInRoot(string root, string stem, string faction)
    {
        if (!Directory.Exists(root)) return null;

        string other = faction == "Witches" ? "Survivors" : "Witches";

        var dirs = new List<string>(4);

        if (!string.IsNullOrEmpty(faction))
        {
            string own = Path.Combine(root, faction);
            if (Directory.Exists(own)) dirs.Add(own);
        }
        dirs.Add(root);

        foreach (var sub in SubDirectories(root))
        {
            string subName = Path.GetFileName(sub);
            if (string.Equals(subName, other, System.StringComparison.OrdinalIgnoreCase)) continue;
            if (dirs.Contains(sub)) continue;
            dirs.Add(sub);
        }

        for (int i = 0; i < dirs.Count; i++)
        {
            string hit = FindInDirectory(dirs[i], stem);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>Exact stem match in one directory (case-insensitive, any image format).</summary>
    private static string FindInDirectory(string dir, string stem)
    {
        var files = FilesIn(dir);
        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i];
            if (!IsImageFile(file)) continue;

            string fileStem = Normalize(Path.GetFileNameWithoutExtension(file));
            if (fileStem == stem) return file;
        }
        return null;
    }

    private static bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return false;
        ext = ext.ToLowerInvariant();
        for (int i = 0; i < Extensions.Length; i++)
            if (ext == Extensions[i]) return true;
        return false;
    }

    private static List<string> FilesIn(string dir)
    {
        List<string> files;
        if (_dirCache.TryGetValue(dir, out files)) return files;

        files = new List<string>();
        try
        {
            if (Directory.Exists(dir))
            {
                files.AddRange(Directory.GetFiles(dir));
                // .png before .jpg before .jpeg, then alphabetical: the same
                // picture dropped twice always resolves the same way.
                files.Sort(CompareImageFiles);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CHARACTER IMAGE] Cannot list " + dir + ": " + e.Message);
        }

        _dirCache[dir] = files;
        return files;
    }

    private static int CompareImageFiles(string a, string b)
    {
        int ra = FormatRank(a);
        int rb = FormatRank(b);
        if (ra != rb) return ra.CompareTo(rb);
        return string.Compare(Path.GetFileNameWithoutExtension(a), Path.GetFileNameWithoutExtension(b),
            System.StringComparison.OrdinalIgnoreCase);
    }

    private static int FormatRank(string path)
    {
        string ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return 9;
        ext = ext.ToLowerInvariant();
        for (int i = 0; i < Extensions.Length; i++)
            if (ext == Extensions[i]) return i;
        return 9;
    }

    private static List<string> SubDirectories(string dir)
    {
        var result = new List<string>();
        try
        {
            if (!Directory.Exists(dir)) return result;
            var dirs = Directory.GetDirectories(dir);
            System.Array.Sort(dirs, System.StringComparer.OrdinalIgnoreCase);
            result.AddRange(dirs);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CHARACTER IMAGE] Cannot list subfolders of " + dir + ": " + e.Message);
        }
        return result;
    }

    // ---- decoding ----------------------------------------------------------

    private static Texture2D LoadFile(string path, out string error)
    {
        error = null;

        long stamp = 0;
        try { stamp = File.GetLastWriteTimeUtc(path).Ticks; }
        catch { /* file vanished between listing and reading - decoded below */ }

        // One decode per file, even when several characters share it - but only
        // while the file on disk is unchanged. Overwriting a picture with a new
        // one of the same name invalidates the cached texture immediately, so a
        // replacement shows up without restarting the game.
        Texture2D cached;
        if (_fileCache.TryGetValue(path, out cached) && cached != null)
        {
            long known;
            if (!_fileTimes.TryGetValue(path, out known) || known == stamp) return cached;

            _owned.Remove(cached);
            Object.Destroy(cached);
            _fileCache.Remove(path);
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length < 32)
            {
                error = "file is empty";
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!tex.LoadImage(bytes))
            {
                Object.Destroy(tex);
                error = "not a readable PNG/JPG";
                return null;
            }

            tex.name = "CharacterImage_" + Path.GetFileNameWithoutExtension(path);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 0;

            Texture2D final = Downscale(tex, MaxTextureSize);
            if (final != tex) Object.Destroy(tex);

            _owned.Add(final);
            _fileCache[path] = final;
            _fileTimes[path] = stamp;
            return final;
        }
        catch (System.Exception e)
        {
            error = e.Message;
            return null;
        }
    }

    /// <summary>Box-filtered downscale so a 2048 px photo does not cost 16 MB of VRAM.</summary>
    private static Texture2D Downscale(Texture2D src, int maxSize)
    {
        int w = src.width, h = src.height;
        if (w <= maxSize && h <= maxSize) return src;

        float scale = Mathf.Min((float)maxSize / w, (float)maxSize / h);
        int nw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
        int nh = Mathf.Max(1, Mathf.RoundToInt(h * scale));

        Color32[] srcPixels = src.GetPixels32();
        Color32[] dstPixels = new Color32[nw * nh];

        for (int y = 0; y < nh; y++)
        {
            int y0 = y * h / nh;
            int y1 = Mathf.Max(y0 + 1, (y + 1) * h / nh);

            for (int x = 0; x < nw; x++)
            {
                int x0 = x * w / nw;
                int x1 = Mathf.Max(x0 + 1, (x + 1) * w / nw);

                int r = 0, g = 0, b = 0, a = 0, n = 0;
                for (int sy = y0; sy < y1; sy++)
                {
                    int row = sy * w;
                    for (int sx = x0; sx < x1; sx++)
                    {
                        Color32 c = srcPixels[row + sx];
                        r += c.r; g += c.g; b += c.b; a += c.a; n++;
                    }
                }

                dstPixels[y * nw + x] = new Color32(
                    (byte)(r / n), (byte)(g / n), (byte)(b / n), (byte)(a / n));
            }
        }

        var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false, false);
        dst.name = src.name;
        dst.wrapMode = TextureWrapMode.Clamp;
        dst.filterMode = FilterMode.Bilinear;
        dst.anisoLevel = 0;
        dst.SetPixels32(dstPixels);
        dst.Apply(false, false);
        return dst;
    }

    // ---- Resources ---------------------------------------------------------

    /// <summary>
    /// Looks a stem up in the Unity Resources folders. The documented location
    /// Assets/Resources/CharacterImages/{Survivors|Witches} wins over the older
    /// Assets/Resources/CharacterFaces folder. Resources.LoadAll does NOT
    /// recurse into sub-folders, so every folder is indexed explicitly.
    /// </summary>
    private static Texture2D FindResourceForStem(string stem, string faction)
    {
        BuildResourceIndex();

        Texture2D tex;
        if (!string.IsNullOrEmpty(faction))
        {
            if (_resourceIndex.TryGetValue(faction + "/" + stem, out tex) && tex != null) return tex;
        }
        if (_resourceIndex.TryGetValue("root/" + stem, out tex) && tex != null) return tex;
        if (_resourceIndex.TryGetValue("legacy/" + stem, out tex) && tex != null) return tex;
        return null;
    }

    private static void BuildResourceIndex()
    {
        if (_resourceIndex != null) return;

        _resourceIndex = new Dictionary<string, Texture2D>(System.StringComparer.Ordinal);

        // Documented location, highest priority: Survivors and Witches.
        IndexResources("CharacterImages/Survivors", "Survivors");
        IndexResources("CharacterImages/Witches", "Witches");

        // Pictures dropped straight into CharacterImages/ (no sub-folder).
        IndexResources("CharacterImages", "root");

        // Legacy placeholder folder, lowest priority.
        IndexResources("CharacterFaces", "legacy");
    }

    private static void IndexResources(string folder, string bucket)
    {
        Texture2D[] textures;
        try { textures = Resources.LoadAll<Texture2D>(folder); }
        catch { return; }

        if (textures == null) return;
        for (int i = 0; i < textures.Length; i++)
        {
            var tex = textures[i];
            if (tex == null) continue;
            string key = Normalize(tex.name);
            if (string.IsNullOrEmpty(key)) continue;

            string full = bucket + "/" + key;
            if (_resourceIndex.ContainsKey(full)) continue;
            _resourceIndex[full] = tex;
        }
    }

    // ---- logging -----------------------------------------------------------

    private static void LogLoaded(string label, Texture2D tex, string source)
    {
        if (!_logged.Add("loaded|" + label)) return;
        Debug.Log($"[CHARACTER IMAGE] Loaded: {label} ({tex.width}x{tex.height} from {source})");
    }

    private static void LogMissing(string label, string key)
    {
        if (!_logged.Add("missing|" + key)) return;
        Debug.LogWarning($"[CHARACTER IMAGE] Missing image: {label} (looked for '{key}.png/.jpg/.jpeg') -> procedural fallback");
    }

    private static void LogInvalid(string path, string reason)
    {
        if (!_logged.Add("invalid|" + path)) return;
        Debug.LogWarning($"[CHARACTER IMAGE] Invalid image: {path} ({reason})");
    }
}
