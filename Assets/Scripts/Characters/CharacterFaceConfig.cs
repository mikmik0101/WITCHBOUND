/// <summary>
/// File-name aliases for character pictures.
///
/// This class does NOT load anything. CharacterImageLibrary is the only reader
/// of pictures; it asks this class for the short name people actually save art
/// under ("mara_veyne" -> "Mara") so that "Mara.png", "MaraVeyne.png" and
/// "mara_veyne.png" all resolve to the same character.
/// </summary>
public static class CharacterFaceConfig
{
    /// <summary>
    /// Short file name for a character, survivor or witch. Keyed by the stable
    /// internal id, NOT by the display name, so the art shipped under the
    /// original names ("Mara.png", "Ethan.png", "mara_veyne.png") keeps loading
    /// after a rename.
    /// </summary>
    public static string LegacyFileForName(string displayNameOrId)
    {
        if (string.IsNullOrEmpty(displayNameOrId)) return null;

        int idx = CharacterLore.IndexOfAny(displayNameOrId);
        if (idx >= 0) return LegacyFileForId(CharacterLore.All[idx].id);

        return Sanitize(displayNameOrId);
    }

    /// <summary>Short art file name that has always belonged to this id.</summary>
    public static string LegacyFileForId(string id)
    {
        switch (id)
        {
            // survivors
            case "ethan_vale":          return "Ethan";
            case "lucas_hart":          return "Lucas";
            case "adrian_cross":        return "Adrian";
            case "noah_reyes":          return "Noah";
            // witches
            case "mara_veyne":          return "Mara";
            case "seraphine_blackroot": return "Seraphine";
            case "morrow":              return "Morrow";
            case "vespera":             return "Vespera";
            case "mother_grin":         return "MotherGrin";
            case "evelyn_graves":       return "Evelyn";
            default:                    return null;
        }
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return s.Replace(" ", "");
    }
}
