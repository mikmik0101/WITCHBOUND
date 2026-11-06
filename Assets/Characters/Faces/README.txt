WITCHBOUND CHARACTER PICTURES
=============================

Drop one picture per character into ONE of these folders - no code changes,
no scene edits, no inspector work.

BEST (ships inside the build automatically):

    Assets/Resources/CharacterImages/Witches/Mara.png
    Assets/Resources/CharacterImages/Survivors/Ethan.png

ALSO WORKS (copied into the build automatically before every build):

    Assets/Characters/Faces/Witches/Mara.png
    Assets/Characters/Faces/Survivors/Ethan.png
    Assets/Characters/Witches/Mara.png
    Assets/Characters/Survivors/Ethan.png
    Assets/Faces/Witches/Mara.png
    Assets/Faces/Survivors/Ethan.png

ALSO WORKS WITHOUT REBUILDING (for quick testing):

    ~/.config/unity3d/<company>/<product>/CharacterImages/Mara.png

FILE NAME
---------
The file name is matched case-insensitively against any of:

    mara_veyne      (character id)
    Rona Tanyel     (display name)
    MaraVeyne       (no spaces)
    Mara            (short name)

So "Mara.png", "mara_veyne.jpg", "MARA VEYNE.JPEG" and "maraveyne.png" all
find Rona Tanyel. Files that match nothing are simply ignored.

The previous display names (Mara Veyne, Seraphine Blackroot, Morrow, Vespera,
Mother Grin, Evelyn Graves, Ethan Vale, Lucas Hart, Adrian Cross, Noah Reyes)
are still accepted as aliases, so existing art never has to be renamed.

FORMAT
------
PNG, JPG or JPEG - the extension does not have to match the real encoding.
Any size or aspect ratio works: pictures are scaled to at most 1024 px and
cropped to fit (never stretched). Square-ish portraits look best.

    Survivors:  Ethan  Lucas  Adrian  Noah
    Witches:    Mara  Seraphine  Morrow  Vespera  MotherGrin  Evelyn

If a picture is missing or unreadable, WITCHBOUND logs a warning and draws
its built-in procedural face instead - nothing breaks.

Log lines to look for:

    [CHARACTER IMAGE] Loaded: Rona Tanyel (768x1024 from .../Mara.png)
    [CHARACTER IMAGE] Applied gameplay texture: Rona Tanyel
    [CHARACTER IMAGE] Applied library portrait: Rona Tanyel
    [CHARACTER IMAGE] Missing image: Rona Tanyel -> procedural fallback
