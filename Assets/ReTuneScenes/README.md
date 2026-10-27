# Re:Tune scene whitebox

Open `Scenes/00_Start.unity` or use `ReTune > Scenes > Open Start`.

## Scenes

- `00_Start`: main menu.
- `01_Shop`: dialogue and resonance.
- `02_Parts`: drag parts into the rhythm and tone slots, then select Test.
- `03_Performance`: use A/D and the mouse together.
- `04_Reply`: closing dialogue and the next visitor.
- `05_Ending`: the letter and epilogue.

All scenes can be played directly. A direct entry uses the controller's Preview Customer setting. The full flow carries visitor, part and performance data between scenes.

## Edit without Play

All world objects, HUD elements, labels and audio sources are saved in the scenes. Runtime scripts only change values, visibility and transforms, and load scenes. They do not create the world or interface.

Expand `World` to edit the actual 2D game objects:

- Shop backgrounds, windows, shelves, the counter and music box use SpriteRenderer visuals.
- `Portrait_Visitor Name` contains the visitor's world-space portrait. Keep the parent reference when replacing art.
- `Part_Name` is a plain Transform with a drag component, a SpriteRenderer visual and a BoxCollider2D. Dragging uses camera rays and world coordinates, not UI drag rectangles.
- Rhythm and tone slots are world objects. Their visual children contain colliders. A correct drop snaps the existing part into place; replacement returns the previous part to its home. Click to remove or double-click for details.
- The dial, needle, track graphics and mouse hit area are world objects. The mouse target uses a BoxCollider2D for click, hold and exit events.
- Move the parent Transform to move a whole object. Edit `Visual` for its sprite, color, rendered size and collider dimensions. Whitebox sizing uses 100 reference pixels per world unit.

Expand `UI_HUD` to edit screen interface elements: dialogue panels, menu and navigation buttons, instructions, status/progress bars, and the help overlay. These use RectTransform, Text and Image. Saved button events remain configured in the scenes.

Text attached to a world object uses a small World Space Canvas only for its label. It has no GraphicRaycaster; the SpriteRenderer and collider handle the object and its interaction. Labels follow their parent when it moves.

The main camera has a Physics2DRaycaster for world interaction. `ReTuneWorldCamera` matches the HUD's reference sizing so world objects and HUD labels stay aligned. World objects use the Default layer; HUD objects use the UI layer. The raycaster event mask includes only Default. The existing EventSystem routes UI and world pointer events; the modal help overlay blocks interaction.

Edit visitor names, dialogue, replies, emotions, recipes, part descriptions and drift settings in `Data/Story.asset`. Scene dialogue is an edit-time preview; runtime dialogue comes from this asset.

Edit note count, spacing, hit window, hold duration and balance settings on `Scene Controller`.
Replace audio clips on the three saved audio sources. The current visitors share placeholder tracks.

## English text

All game text is in English, including scene previews, runtime hints, story data, help panels and the retained legacy whitebox. Object names and scene generation templates are also English. Prompts use short actions such as Start, Next, Listen, Test, Clear and Retry.

## Existing files and generation

The original SampleScene and `Assets/ReTuneWhitebox/ReTuneWhitebox.unity` remain available. The six new scenes are registered in build settings, with `00_Start` first.

`ReTune > Scenes > Create Missing Scenes (preserve existing)` creates missing scenes only, including their world/HUD split. It preserves existing scenes, story data and audio. Normal editing does not require this command.

`Tools/Convert-ReTuneToWorld.ps1` is the one-time file migration used for the existing six scenes. It skips any scene already containing `World`. It does not start Unity or create another project. The original retained legacy prototype is unchanged by this architecture migration.

## Validation

- Current static checks: scene object references, component ownership, parent/child links, asset GUIDs, required controller fields, build entries, six plain-Transform world parts, eight part/slot colliders, the world note collider, camera raycaster and 23 saved HUD button events. Colliders share their SpriteRenderer object so pointer sorting respects visual order.
- Compilation: runtime and editor scripts compile with Unity's compiler and existing UI assemblies without starting the editor.
- Earlier Play checks: `Logs/ReTuneScenes-final.log` contains `RETUNE_SCENE_FLOW_PASS`, covering four visitor loops, recipes, note methods, stability reset, ending and menu return. This predates the world-object migration and English-text changes; it does not validate the new physics pointer path.
- Earlier screenshots: 1280 x 720 previews were inspected before the English update and world-object migration. The new world/HUD layouts have not been visually verified in Unity.
- Manual checks still needed: real dragging, wrong-slot feedback, replacement, double-clicks, focus loss, mouse holds, simultaneous A/D input, fast scene changes, resolution changes and audio.

Run `./Tools/Validate-ReTuneScenes.ps1` from the project root for read-only static checks. It does not start Unity, open ports, modify scenes or create project copies. Asset checks use the existing `Library/PackageCache`.

Final art, complete dialogue, visitor-specific audio, saving and final note charts remain future work.
