# Joburg Street Run

`JoburgEndlessRunner` is the official main game in this workspace.

## Project Status

- Engine: Unity `6000.5.2f1`
- Main scene: `Assets/Scenes/JoburgEndlessRunner.unity`
- Main build output: `Builds/JoburgEndlessRunner.apk`
- Preview image: `Builds/preview.png`
- Editor build menu: `Joburg Runner > Build Minimum Playable Scene`
- Android build menu: `Joburg Runner > Build Android APK`

## Unity Structure

- `Assets/Scripts` contains the active runner gameplay code.
- `Assets/Scripts/Editor/EndlessRunnerSceneBuilder.cs` regenerates the official scene, prefabs, materials, and build settings.
- `Assets/Scenes/JoburgEndlessRunner.unity` is the scene to open, test, and build.
- `Assets/Prefabs`, `Assets/Materials`, `Assets/Textures`, `Assets/Meshes`, and `Assets/Animations` are active game assets.
- `Assets/FPS` is Unity FPS Microgame reference/tutorial content.
- `Assets/JoziGame` is an earlier experimental Jozi scene and should not be treated as the current main game.
- `quickwin_planner` is a separate Flutter app and is not part of the Unity runner.

## Current Gameplay Loop

The player runs forward through three lanes, avoids oncoming taxis, collects coins, and builds score from distance. Speed and obstacle pressure increase during a run. Game over saves high score and total coins, then allows restart from the restart button, keyboard, or touch.

## Recommended Next Cleanup

After the current runner loop feels good on device, move legacy Unity content into a separate archive project or Git branch. Keeping it in-place for now preserves Unity asset GUIDs and avoids breaking scene references.
