# SoundFonts

Singularidi Pro needs a SoundFont (`.sf2`) to produce audio. SoundFonts are not committed to this repository for license and size reasons; you must supply one before the audio engine can play anything.

## How `PlaybackHost` discovers a SoundFont

At runtime `MeltySynthAudioEngine` resolves a SoundFont in this order:

1. **`AppConfig.SoundFontPath`** — if set and the file exists, it wins. This is the persisted user preference, stored in `Application.persistentDataPath/config.json`. Configure it via the in-app settings (Phase 4 transport UI) or by editing the JSON.
2. **`Assets/StreamingAssets/SoundFonts/*.sf2`** — first `.sf2` found alphabetically is used as a fallback. This is the easiest path for development and verification: drop any `.sf2` file in this folder and the editor / standalone build will find it.
3. **Hard error.** If neither path yields a `.sf2`, the audio engine logs both checked locations and refuses to start. Look for `[MeltySynthAudioEngine] No SoundFont found` in the Unity console.

## Recommended public-domain SoundFonts

| Name | Size | License | Notes |
|---|---|---|---|
| **GeneralUser GS v1.471** | ~32 MB | CC-BY 3.0 (S. Christian Collins) | Standard recommendation. Excellent GM coverage; good piano. Download at <http://schristiancollins.com/generaluser.php>. |
| **FluidR3_GM** | ~150 MB | MIT | Larger, broader instrument coverage. Bundled with FluidSynth. |
| **TimGM6mb** | ~6 MB | GPL-style | Tiny; useful only for quick tests. |

Drop the chosen `.sf2` into this folder. Unity will import it as a binary asset (no special importer required). The file will be ignored by source control if you keep `.sf2` in `.gitignore`, or commit it via LFS if you want the team to share one.

## When this fails

If the verification scene plays silence:

1. Check the Unity console for the `[MeltySynthAudioEngine]` log line — it names exactly which paths it checked.
2. Confirm at least one `.sf2` is in this folder, or that `AppConfig.SoundFontPath` points at an existing file.
3. Confirm the file is not zero bytes (a stub or LFS pointer that wasn't fetched will read as zero bytes; run `git lfs pull` if needed).

## Why a fallback at all?

The legacy app (`singularidi/`) required users to configure a SoundFont path explicitly. Picking up a `.sf2` from this folder gives a future developer a zero-setup verification path — drop any file in here and Play just works.
