WITCHBOUND - CHARACTER PICTURES
==============================================================================

ONE folder. Ten file names. No C# editing, no Inspector work, no scene edits.


------------------------------------------------------------------------------
 1. THE ONLY FOLDER YOU NEED
------------------------------------------------------------------------------

    Assets/Resources/CharacterImages/Survivors/
    Assets/Resources/CharacterImages/Witches/

  Survivors/  -> the four playable survivors
  Witches/    -> the six witches

That is the single official source. The game loads every portrait from here,
and the Linux build copies this folder into itself when it is built.

Ignore every other picture folder in the project (Assets/Characters/...,
Assets/Faces/..., Assets/Resources/CharacterFaces/...). Those are old drop
folders kept only so ancient art does not break. Art placed there is NOT
picked up ahead of this folder.


------------------------------------------------------------------------------
 2. THE EXACT FILE NAMES  (all ten)
------------------------------------------------------------------------------

  Assets/Resources/CharacterImages/Survivors/

    luis-padilla.png      Luis Padilla      skill: Phase
    mick-richards.png     Mick Richards     skill: Emergency Weapon
    josh-garcia.png       Josh Garcia       skill: Speed Burst
    vincent-reid.png      Vincent Reid      skill: Invisibility

  Assets/Resources/CharacterImages/Witches/

    rona-tanyel.png       Rona Tanyel       (Hollow Witch)
    demonisa-belle.png    Demonisa Belle    (Winged Witch)
    kyla-ni-garcia.png    Kyla Ni-Garcia    (The Hound)
    jannak.png            Jannak            (Crawling Witch)
    sandy-vill.png        Sandy Vill        (Tall Witch)
    beamoth.png           Beamoth           (Final Witch)

Spelling and the hyphens matter. "demonysa-belle.png" will NOT be found.
Lower case is recommended but not required: file names are matched without
regard to case, spaces, underscores or hyphens, so "Luis Padilla.PNG" and
"luis-padilla.png" resolve to the same character.

Replacing a file keeps its name: overwrite the existing file, do not add
"luis-padilla-2.png".


------------------------------------------------------------------------------
 3. SUPPORTED FORMATS
------------------------------------------------------------------------------

    .png      recommended (keeps transparency, no artefacts)
    .jpg      fine for photographs
    .jpeg     fine for photographs

Upper case extensions work too (.PNG, .JPG, .JPEG). Keep the exact file name
stem and change only the extension if you prefer JPG; if both a .png and a
.jpg exist for the same character, the .png wins.

Recommended picture: 512x512 or 1024x1024, upright, cropped to head and
shoulders. Anything larger is downscaled to 1024 automatically. The game is
dark, so keep the face reasonably lit. A 3:4 portrait is cropped, never
squashed.


------------------------------------------------------------------------------
 4. HOW TO ADD OR REPLACE A PHOTO
------------------------------------------------------------------------------

  1. Copy your picture over the matching file above, keeping the exact name.
     Example: overwrite
         Assets/Resources/CharacterImages/Witches/jannak.png
     with your own picture of Jannak.

  2. Rebuild the game (see section 5). Nothing else to do - import settings
     are applied automatically, and no code knows any character by file name
     at compile time.

You can replace one picture at a time. A character without a picture is not
an error: it falls back to a generated placeholder portrait.


------------------------------------------------------------------------------
 5. DO I HAVE TO REBUILD?
------------------------------------------------------------------------------

    In the Unity EDITOR (Play):     NO.  The editor reads the file from this
                                        folder directly, so a replaced picture
                                        appears the next time the portrait is
                                        drawn (re-enter the menu, or restart
                                        Play mode).

    In the BUILT Linux game:        YES. Pictures are baked into the build.
                                        Run  WITCHBOUND > Build Linux  again.

  Note: the built game also keeps a plain copy of these ten files at
      Builds/WITCHBOUND_Data/StreamingAssets/CharacterImages/{Survivors,Witches}/
  so you can swap a photo there and just restart the game if you only want to
  try something out. The project folder above is still the real source - a
  rebuild overwrites that copy from it.


------------------------------------------------------------------------------
 6. WHERE THE PICTURE IS USED
------------------------------------------------------------------------------

One resolver feeds all three places, so a character can never show two
different pictures:

    Character Select card      (survivor portrait + chaser portrait)
    Character Library          (all ten profiles)
    In-game 3D character head  (player, companions and every witch)

Pictures are cosmetic only. They never change AI, speed, health, hitboxes,
colliders, spawns or difficulty.


------------------------------------------------------------------------------
 7. HOW TO VERIFY A PICTURE LOADED
------------------------------------------------------------------------------

In the game:
    Main menu > CHARACTER LIBRARY  - shows all ten portraits.
    Main menu > PLAY > Character Select - shows the four survivor portraits.
    In a round, the 3D heads use the same pictures.

In the log (Player.log for the build, Console in the editor) the game prints
one line per character the first time it resolves:

    [CHARACTER IMAGE] Loaded: Luis Padilla (512x512 from .../luis-padilla.png)
    [CHARACTER IMAGE] Applied character-select portrait: Luis Padilla
    [CHARACTER IMAGE] Applied library portrait: Luis Padilla
    [CHARACTER IMAGE] Applied gameplay texture: Luis Padilla

"from .../CharacterImages/Survivors/luis-padilla.png" means your file was
used. "from Resources" means the build's baked copy was used (same picture,
just inside the build).

When a character has no picture at all:

    [CHARACTER IMAGE] Missing image: Luis Padilla -> procedural fallback

That warning is the only case where placeholder art appears.


------------------------------------------------------------------------------
 8. WHAT IS STILL PLACEHOLDER
------------------------------------------------------------------------------

No photograph is ever invented for this project. If a character still shows
the generated card art, its file in this folder is still the placeholder - the
pixels literally say "PLACEHOLDER - REPLACE ME". Open the file and look: if it
says that, no photo has been supplied for that character yet.

    Survivors/*  - currently still the labelled placeholder art
    Witches/*    - currently real photographs

==============================================================================
