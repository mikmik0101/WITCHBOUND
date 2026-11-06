using UnityEngine;

/// <summary>
/// One browsable Character Library entry. Pure data: nothing here is read by
/// gameplay, so editing lore can never affect AI, colliders or balance.
/// </summary>
public struct CharacterEntry
{
    public string id;          // stable face identifier, e.g. "ethan_vale"
    public string name;
    public string role;        // survivor role, or the witch's title
    public string traits;
    public string bio;
    public string threat;      // observed behaviour in the chase
    public bool isMonster;
    public WitchType witchType; // used for the procedural fallback portrait

    public CharacterEntry(string id, string name, string role, string traits,
        string bio, string threat, bool isMonster, WitchType witchType)
    {
        this.id = id;
        this.name = name;
        this.role = role;
        this.traits = traits;
        this.bio = bio;
        this.threat = threat;
        this.isMonster = isMonster;
        this.witchType = witchType;
    }
}

/// <summary>
/// The complete cast of WITCHBOUND: four survivor researchers and the six
/// witches of Ashwood. This is the single identity source the Character Library
/// and Character Select read from; the floating name tags use the same names
/// through CharacterVisual.displayName.
/// </summary>
public static class CharacterLore
{
    public static readonly CharacterEntry[] All =
    {
        // ============================================================ survivors
        new CharacterEntry(
            "ethan_vale", "Luis Padilla", "Researcher   //   Survivor",
            "Responsible   //   Observant   //   Determined",
            "Luis came to Ashwood to document a mill collapse and stayed to draw the streets that the county map insists are not there. " +
            "He was the first to notice the roads repeating - the same crooked fence, the same dead streetlight, the same wet leaves - " +
            "and the first to understand what that meant. He keeps the group moving because stopping to think is a luxury the village does not allow. " +
            "Calm under pressure, he would rather be proved wrong than be right alone.",
            "AI companion. Holds the group together, mirrors your running speed and breaks for the nearest gap when a witch closes in.",
            false, WitchType.Humanoid),

        new CharacterEntry(
            "lucas_hart", "Mick Richards", "Researcher   //   Survivor",
            "Intelligent   //   Curious   //   Cautious",
            "Mick documents what other people refuse to look at twice. He arrived with a case of blank notebooks and the cheerful certainty " +
            "that Ashwood was just another research mystery with a tidy explanation at the end. The first night in the fog took that certainty apart. " +
            "He still writes everything down, but he no longer reads his notes back aloud, and he has stopped asking what the shapes in the street are.",
            "AI companion. Stays close to the group, keeps his distance from the fog and freezes only for as long as it takes to run.",
            false, WitchType.Humanoid),

        new CharacterEntry(
            "adrian_cross", "Josh Garcia", "Researcher   //   Survivor",
            "Brave   //   Loyal   //   Stubborn",
            "Josh is the one who goes back. He is stronger than the rest of the team and knows it, and he has never once accepted " +
            "that leaving someone behind is a reasonable price for getting out. In Ashwood that instinct is both the group's best chance " +
            "and its worst habit: he will step between a companion and a witch without checking whether the street behind him is clear.",
            "AI companion. Physically the steadiest of the three; he breaks off to reach a downed teammate instead of running for himself.",
            false, WitchType.Humanoid),

        new CharacterEntry(
            "noah_reyes", "Vincent Reid", "Researcher   //   Survivor",
            "Analytical   //   Resourceful   //   Skeptical",
            "Vincent came for the signal. The county relay above Ashwood had been broadcasting a carrier wave with nothing on it for eleven years, " +
            "and he wanted to know what was holding the channel open. He suspects the village is not haunted but built - a loop with walls " +
            "he has not found yet - and he keeps testing that theory even when the test is running down a street that should not exist.",
            "AI companion. Reads the street instead of the monsters; he keeps moving when the others hesitate and rarely gets cornered.",
            false, WitchType.Humanoid),

        // ============================================================= witches
        new CharacterEntry(
            "mara_veyne", "Rona Tanyel", "The Hollow Witch",
            "Patient   //   Relentless   //   Unsettling",
            "Rona was married in the Ashwood chapel in the autumn of 1974. The groom never arrived and the congregation left at midnight, " +
            "but the bride stayed, and the town learned not to walk the main road after dark. Her dress has not yellowed in all the years since. " +
            "She is the oldest of the six and the quietest: people describe the cold at the end of a street before they describe her.",
            "Ground hunter. Approaches along open streets at a steady pace, strikes from close range and never willingly gives up the chase.",
            true, WitchType.Humanoid),

        new CharacterEntry(
            "seraphine_blackroot", "Demonisa Belle", "The Winged Witch",
            "Predatory   //   Watchful   //   Merciless",
            "Demonisa was the mill's night nurse until the fire took the lower half of her body and, by every account, most of her mind. " +
            "What climbed out of the ash does not walk. It hangs above the rooftops on wet grey wings, patient as a weather vane, " +
            "and the oldest legends in the county describe exactly that silhouette circling the steeple long before the mill ever burned.",
            "Airborne hunter. Hovers above the street and strikes from further out than any of the others - the longest reach of the six.",
            true, WitchType.Winged),

        new CharacterEntry(
            "morrow", "Kyla Ni-Garcia", "The Hound",
            "Aggressive   //   Instinctive   //   Relentless",
            "Kyla belonged to the groundskeeper and was buried behind the chapel with a name tag and a tin bowl. Three winters later " +
            "the bowl was found empty and the grave was open. It runs low, close to the asphalt, and it does not need to see you " +
            "to know which way you went; it only needs to hear you decide to run.",
            "Quadruped. Lowest profile and the quickest to close distance, but the shortest reach - it has to be almost on top of you.",
            true, WitchType.Hound),

        new CharacterEntry(
            "vespera", "Jannak", "The Crawling Witch",
            "Cunning   //   Unpredictable   //   Patient",
            "Jannak was the town's seamstress, and the town still finds her thread in the strangest places: tied to drainpipes, " +
            "strung between fence posts, wound around the handles of doors that were locked from the inside. She does not stand. " +
            "She comes along the ground with her arms folded under her and waits in the narrow gaps between buildings, " +
            "where a running person cannot see her until it is far too late.",
            "Crawler. Stays low and short-reached, but hard to spot in alleys and doorways - check your corners, not the skyline.",
            true, WitchType.Crawler),

        new CharacterEntry(
            "mother_grin", "Sandy Vill", "The Tall Witch",
            "Cruel   //   Intimidating   //   Persistent",
            "Nobody in Ashwood agrees on who Sandy Vill was, only that she was tall and that she smiled at the evacuation buses. " +
            "She is taller now. Her head clears the second-storey windows, her silhouette shows up between buildings where no silhouette " +
            "should fit, and she turns corners with the patience of something that has nowhere else to be.",
            "Tallest of the six. Long stride and the widest swing; easy to outrun in the open, very hard to slip past in a narrow street.",
            true, WitchType.Tall),

        new CharacterEntry(
            "evelyn_graves", "Beamoth", "The Final Witch",
            "Intelligent   //   Manipulative   //   Mysterious",
            "Beamoth signed the town's closure papers and then signed nothing else. She is the reason the roads end where they do " +
            "and the reason the county stopped answering the phone. She does not chase so much as arrive, always at the far end of the street " +
            "you were about to take, and she is the only one of the six who looks like she knows how this ends.",
            "Final witch. Balanced speed and reach; she is never in a hurry, and she is never far behind.",
            true, WitchType.Final)
    };

    /// <summary>
    /// Previous official display names, parallel to <see cref="All"/>. These are
    /// NEVER shown to the player: they only let saved sessions, older serialized
    /// assets and picture files named after the old names keep resolving to the
    /// right character after the rename.
    /// </summary>
    private static readonly string[] LegacyNames =
    {
        "Ethan Vale", "Lucas Hart", "Adrian Cross", "Noah Reyes",
        "Mara Veyne", "Seraphine Blackroot", "Morrow", "Vespera", "Mother Grin", "Evelyn Graves"
    };

    /// <summary>
    /// THE OFFICIAL PICTURE FILE NAMES, parallel to <see cref="All"/>.
    ///
    /// These are the exact file names a picture must have to be picked up by the
    /// game. They are keyed to the stable internal id, never to the display
    /// name, so renaming a character can never break its picture.
    ///
    ///   Survivors: Assets/Resources/CharacterImages/Survivors/&lt;file&gt;
    ///   Witches:   Assets/Resources/CharacterImages/Witches/&lt;file&gt;
    ///
    /// Only the stem is stored here; .png, .jpg and .jpeg are all accepted.
    /// </summary>
    private static readonly string[] OfficialFiles =
    {
        // survivors
        "luis-padilla", "mick-richards", "josh-garcia", "vincent-reid",
        // witches
        "rona-tanyel", "demonisa-belle", "kyla-ni-garcia", "jannak", "sandy-vill", "beamoth"
    };

    public static int Count { get { return All.Length; } }

    /// <summary>Number of survivor entries (the library header uses this).</summary>
    public static int SurvivorCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (!All[i].isMonster) n++;
            return n;
        }
    }

    /// <summary>Number of witch entries (the library header uses this).</summary>
    public static int WitchCount { get { return All.Length - SurvivorCount; } }

    public static CharacterEntry Get(int index)
    {
        if (index < 0 || index >= All.Length) return All[0];
        return All[index];
    }

    public static int IndexOfId(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return i;
        return -1;
    }

    /// <summary>Exact current display name match.</summary>
    public static int IndexOfName(string name)
    {
        return IndexOfAny(name);
    }

    /// <summary>
    /// Resolve an id, a current display name or a previous display name to its
    /// roster index. Case, spaces, underscores and punctuation are ignored on the
    /// fallback pass, so "mara_veyne", "MaraVeyne" and "Mara Veyne" all land on
    /// the same character.
    /// </summary>
    public static int IndexOfAny(string value)
    {
        if (string.IsNullOrEmpty(value)) return -1;

        for (int i = 0; i < All.Length; i++)
            if (All[i].id == value || All[i].name == value || LegacyNames[i] == value) return i;

        string compact = Compact(value);
        if (compact.Length == 0) return -1;

        for (int i = 0; i < All.Length; i++)
            if (Compact(All[i].id) == compact || Compact(All[i].name) == compact || Compact(LegacyNames[i]) == compact)
                return i;

        return -1;
    }

    /// <summary>
    /// The current official display name for an id or a previous name. Unknown
    /// values are returned unchanged, so callers can pass through free text.
    /// </summary>
    public static string OfficialName(string value)
    {
        int idx = IndexOfAny(value);
        return idx >= 0 ? All[idx].name : value;
    }

    /// <summary>The previous display name for an index (compatibility only).</summary>
    public static string LegacyNameAt(int index)
    {
        return (index >= 0 && index < LegacyNames.Length) ? LegacyNames[index] : null;
    }

    /// <summary>
    /// The OFFICIAL picture file stem for an index, e.g. "luis-padilla".
    /// Null for anything that is not on the roster.
    /// </summary>
    public static string OfficialFileAt(int index)
    {
        return (index >= 0 && index < OfficialFiles.Length) ? OfficialFiles[index] : null;
    }

    /// <summary>The official picture file stem for an id or a name.</summary>
    public static string OfficialFileFor(string idOrName)
    {
        int idx = IndexOfAny(idOrName);
        return idx >= 0 ? OfficialFileAt(idx) : null;
    }

    /// <summary>
    /// True when the character belongs in the Witches picture folder.
    /// </summary>
    public static bool IsMonsterAt(int index)
    {
        return (index >= 0 && index < All.Length) && All[index].isMonster;
    }

    private static string Compact(string value)
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

    /// <summary>Survivor names in the order Character Select uses.</summary>
    public static readonly string[] SurvivorNames = { "Luis Padilla", "Mick Richards", "Josh Garcia", "Vincent Reid" };

    /// <summary>
    /// The skill each survivor starts with, parallel to <see cref="All"/>.
    /// Only the four survivor entries are meaningful; witches return Phase by
    /// accident of indexing and are never given a skill.
    ///
    /// This is the single mapping the whole game reads (Character Select,
    /// Character Library, the skill controller and the HUD), so the panel text
    /// and the ability that actually fires can never disagree.
    /// </summary>
    private static readonly SkillType[] SurvivorSkills =
    {
        SkillType.Phase,            // Luis Padilla   - Phase Shift
        SkillType.EmergencyWeapon,  // Mick Richards  - Emergency Weapon
        SkillType.SpeedBurst,       // Josh Garcia    - Speed Burst
        SkillType.Invisibility      // Vincent Reid   - Invisibility
    };

    /// <summary>Skill for a roster index. Witches have none; returns Phase.</summary>
    public static SkillType SkillAt(int index)
    {
        if (index < 0 || index >= SurvivorSkills.Length) return SkillType.Phase;
        return SurvivorSkills[index];
    }

    /// <summary>Skill for an id or a name (current or previous).</summary>
    public static SkillType SkillFor(string idOrName)
    {
        int idx = IndexOfAny(idOrName);
        return idx >= 0 ? SkillAt(idx) : SkillType.Phase;
    }

    /// <summary>
    /// Human-readable name of the skill a survivor starts with, for menus.
    /// </summary>
    public static string SkillNameFor(string idOrName)
    {
        switch (SkillFor(idOrName))
        {
            case SkillType.Phase: return "PHASE SHIFT";
            case SkillType.EmergencyWeapon: return "EMERGENCY WEAPON";
            case SkillType.SpeedBurst: return "SPEED BURST";
            case SkillType.Invisibility: return "INVISIBILITY";
            default: return "SKILL";
        }
    }
}
