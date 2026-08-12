# Custom Blocks
This is an `Ultimate Chicken Horse` `BepInEx` mod that adds custom blocks and allows you place pieces in the background.
Currently many new custom blocks are work in progress, so expect things to be broken.

# Background Blocks
Allows you place pieces in the background.
Works with online saves, but background pieces will not appear for users without the mod.


https://user-images.githubusercontent.com/1382274/178787565-84d55c33-fe85-42cb-bc55-a39406c9480a.mp4

| Key          |  Overrides                         |
| ---          |                                --- |
| G            | Toggle Mod Block Mode              |
| L            | Switch to next layer               |
| Shift + L    | Switch to previous layer           |
| H            | Highlight blocks on current layer  |


Pressing `G` in Free Play place phase (build modus) toggles the Background Modus, if it is enabled all blocks places will be background blocks.

(Keybindings can be changed in the config file `BepInEx\config\CustomBlocks.cfg`.)


# Custom Blocks

## Making your own blocks (other mods)

Other BepInEx mods can add blocks by referencing `CustomBlocksMod.dll` and
registering a `CustomBlock` subclass during their plugin's `Awake`:

```csharp
using CustomBlocks.Core;

class MyBlock : CustomBlock
{
    // vanilla block to clone
    public override int BasedId { get { return 0; } }
    public override string BasePlaceableName { get { return "01_1x1 Box"; } }
    public override string BasePickableBlockName { get { return "01_1x1 Box_Pick"; } }

    // sprite is loaded from <your plugin dir>/assets/MyBlock.png
    public override Rect SpriteRect { get { return new Rect(0, 0, 54, 54); } }
}

// in your BaseUnityPlugin.Awake:
CustomBlockRegistry.Register<MyBlock>();
```

Block identity in saves comes from a stable id derived from the class name,
so save files keep working regardless of which other block mods are installed
or in which order they register.

## FloatyCloud
A cloud that starts sinking when a player steps on it.
If the cloud losses too much height, it start to go transparent and players can no longer stand on it.


## MultiStart
A block that allows for multi start positions to be placed in one level.


## OneRoundWood 
1x1 wood block with rounded edges.


## ReCoin
A coin that respawn in the next round after being collect. 


## RemoteControl
Two blocks a receiver and transmitter that allows remotely controlling certain blocks.


## Credits
- [Clever Endeavour Games](https://www.cleverendeavourgames.com/)
- [BepInEx](https://github.com/BepInEx/BepInEx) team
- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike
