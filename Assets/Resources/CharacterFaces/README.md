# WITCHBOUND — Character Faces (legacy folder)

**This folder is no longer the main picture location.** All ten official
pictures now live in:

```
Assets/Resources/CharacterImages/Survivors/
Assets/Resources/CharacterImages/Witches/
```

See `Assets/Resources/CharacterImages/README.txt` for the exact file names,
formats, sizes and the replacement procedure.

## What this folder still does

It is kept as a **backward-compatibility fallback**. If a picture is dropped
here under an old name (`ethan_vale.png`, `mara_veyne.png`, …) it still loads
and still resolves to the right character — but a picture in
`Assets/Resources/CharacterImages/` always wins over one here.

The placeholder PNGs that used to sit here have been moved to the official
folder under the official names (e.g. `ethan_vale.png` → `luis-padilla.png`),
so nothing is duplicated in the build.

## Legacy file names that still resolve

| Old file | Character |
|---|---|
| `ethan_vale.png` | Luis Padilla |
| `lucas_hart.png` | Mick Richards |
| `adrian_cross.png` | Josh Garcia |
| `noah_reyes.png` | Vincent Reid |
| `mara_veyne.png` | Rona Tanyel |
| `seraphine_blackroot.png` | Demonisa Belle |
| `morrow.png` | Kyla Ni-Garcia |
| `vespera.png` | Jannak |
| `mother_grin.png` | Sandy Vill |
| `evelyn_graves.png` | Beamoth |

Old display names ("Ethan Vale", "Mara Veyne", "Mother Grin", …) and the short
art names ("Ethan", "Mara", "MotherGrin", …) are accepted as aliases too.
