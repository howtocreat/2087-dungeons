# 2087 Dungeons

A top-down survival adventure set in a post-apocalyptic underground city. Explore the shelter, mine for resources, trade at the market, and fight through the quarantine zone.

## Origin

[howtocreat](https://github.com/howtocreat) created 2087 Dungeons as a hand-written Python text game. The Unity remake brings its underground city, mining robots, trading, and infected enemies into a playable 3D world. This repository includes the [original Python source](Original/地下城1.3.4-翰.py).

## Play on Windows

1. Download the Windows x64 archive from the [Releases page](https://github.com/howtocreat/2087-dungeons/releases).
2. Extract the entire archive and double-click `开始游戏.cmd`.
3. Create a save to begin the story and tutorial, or select an existing save to continue. Use **Import Save** (`导入存档`) to load a save file from another installation.

## Open in Unity

1. Add this repository's folder to Unity Hub and open it with **Unity 6000.6.9f1**.
2. Open `Assets/Scenes/2087.unity` and click **Play**.
3. To make a Windows build, choose **2087 → Build Windows player** in the editor. The build goes into `Builds/Windows`, where the root launcher expects it.

The project uses built-in Unity packages. The game interface and story dialogue are in Simplified Chinese.

## Controls

- **WASD or arrow keys** to move.
- **E** to talk, trade, or use a nearby door. Hold **E** at an ore vein to keep mining.
- **B** to open the backpack.
- **Mouse** to aim, **left click** to fire, and **Space** to dodge.
- **Esc** to pause, save, or return to the main menu.

The game saves progress automatically. You can find your JSON saves through the folder button on the main menu. Importing a save creates a separate slot. Reloading a save made during a fight starts at the pre-battle checkpoint.
