# Shardpunk - TweaksPunk

## Description

This mod add some gameplay changes:

- Cell upgrade no longuer gives overheat chance reduction but instead overheat damage reduction (4 damages with no upgrade, 3 damages with 1 and 2 upgrades and 2 damages with 3 upgrades)
- A dread point is now given each 4 points of stress (previously 5)
- Quirk from dread are now more frequent
- Warcry from the commander rat now gives 2 points of stress for each human character

## Installation

This mod was made with `BepInEx` which is why you need to install it first.

### Installing BepInEx

- Download the version `5.4.22` of [BepInEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22).
- Unzip it in the `Shardpunk` folder.

### Installing BiggerTeam

- Download the latest release of [TweaksPunk](https://github.com/remyCases/Shardpunk-TweaksPunk/releases).
- Move it in the `Shardpunk/BepInEx/plugins` folder.

You can now play the game with the modded version !

## Troubleshooting

If you encountered some troubles while playing with this mod, you can contact me on Discord (zizabot), and send me the log file `LogOutput` found in the folder `Shardpunk/BepInEx`.

## For anyone who wants to change this mod and build it themself

> [!CAUTION]
> The current repo can not be built directly as I didn't include the Assembly dll of Shardpunk (for obvious reasons).
> You need to create a `lib` folder first and to move the `Assembly-CSharp.dll` from the `Shardpunk/Shardpunk_Data/Managed` folder to the `lib` folder.

This mod was made using `HarmonyX`, a fork of `Harmony2` from `BepInEx`. I strongly advise anyone who wants to change the mod to check the documentation of [Harnomy2](https://harmony.pardeike.net/articles/intro.html) and to check the difference between [Harmony2 and HarmonyX](https://github.com/BepInEx/HarmonyX/wiki/Difference-between-Harmony-and-HarmonyX) first.

## See also

Other mods I've made:

- Shardpunk:
    - [Shardpunk-Faster](https://github.com/remyCases/Shardpunk-Faster)
    - [Shardpunk-MoreSkillLevels](https://github.com/remyCases/Shardpunk-MoreSkillLevels)
    - [Shardpunk-RandomParty](https://github.com/remyCases/Shardpunk-RandomParty)
    - [Shardpunk-BiggerTeam](https://github.com/remyCases/Shardpunk-BiggerTeam)

- Stoneshard:
    - [Character Creation](https://github.com/remyCases/CharacterCreator)
    - [Speedshard_Core](https://github.com/remyCases/SpeedshardCore)
    - [Speedshard_Sprint](https://github.com/remyCases/SpeedshardSprint)
    - [Speedshard_Backpack](https://github.com/remyCases/SpeedshardBackpack)
    - [Speedshard_Skinning](https://github.com/remyCases/SpeedshardSkinning)
    - [Speedshard_MoneyDungeon](https://github.com/remyCases/SpeedshardMoneyDungeon)
    - [Speedshard_Stances](https://github.com/remyCases/SpeedshardStances)

- Airship Kingdom Adrift:
    - [ProductionPanel](https://github.com/remyCases/AKAMod_ProdPanel)