# Background Blocks
This is an `Ultimate Chicken Horse` `BepInEx` mod that allows you place pieces in the background.
Works with online saves, but background pieces will not appear for users without the mod.

https://user-images.githubusercontent.com/1382274/169524818-4b661eb7-632f-48a8-908c-9b31ed41bbb3.mp4


| Key  |  Overrides |
| ---  |        --- |
| G    | Background |




Pressing `G` in Free Play place phase (build modus) toggles the Background Modus, if it is enabled all blocks places will be background blocks.

## Build
- Set `PropertyGroup` `UCHfolder` in `BackgroundBlocks.csproj` to point to your correct Ultimate Chicken Horse game folder.

      <PropertyGroup>
        <UCHfolder>C:\Program Files (x86)\Steam\steamapps\common\Ultimate Chicken Horse\</UCHfolder>
      </PropertyGroup>

- Make sure you have BepInEx installed:
  - Download [BepInEx](https://github.com/BepInEx/BepInEx/releases) for your platform (UCH is a x64 program)
  - Put all the contents inside the zip file into your `Ultimate Chicken Horse` folder found via `Steam -> Manage -> Browse Local Files`.


- build with Visual Studio or from the command line:

      dotnet build


## Credits
- [Clever Endeavour Games](https://www.cleverendeavourgames.com/)
- [BepInEx](https://github.com/BepInEx/BepInEx) team
- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike
