# TODO List:
## Features
* **Adding enemies to a zone (Zone Settings Config)**
* **Spawning enemies in a specific room (Room Settings Config)**
  * This requires AWO support, it should be implemented behind the scenes through the SpawnHibernateInZone and SpawnScoutInZone events.
* **Checkpoints (Security Door Config)**
* **Snatcher boxes**
  * Three options for this: Manual addition in the Level Config dialog, automatic addition if snatchers are anywhere in the level, or just add to all levels.
* `LockedNoKey` doors should default the custom text to the vanilla "lockdown" text (TextID 972)
* **Tampered Data Cube events**
* **'Import from vanilla' for fog and lighting settings**
  * as has already been done for the alarms tab
* **Forced Plugs**
  * fields in LGTA are NorthPlug, EastPlug, SouthPlug, WestPlug. empty for no override
* **Ability to search the user docs for keywords**

*If we are wanting to add support for modded enemies and weapons, then:*

* **Custom Enemies (HARD)**
  * Might be best to leave this as only basic config options for new enemies.
* **Custom Weapons (HARD)**
  * This is a long-term addition. It would be sick to have a 3D pannable editor to actually build the guns from the parts and have the json file be created through that.

## Testing
* Like every objective type needs extensive testing
* Blood doors
* Dimensions
  * Might also want to test sectors in other dimensions just in case
* Chained Objectives
* Need to thoroughly test just about everything with levels. When we get close to being finished, we should just make entire rundowns that use just about every feature for testing.

## Known Bugs
* Room expansion from the elevator tile should not be able to be a security door
* The `ActivateHSU` objective in the test level just shits itself when you finish the interaction on it, its likely that just about every objective is cooked but they are untested
* The Rundown tier `Scale` value does not look the same in-game as it does in the editor when there is only one level in the tier - the editor has it look centered even when a non-zero value is entered, but in-game, it is offset by that value.

## Modded Support
Support for mods should be implemented as its own tab with toggles for each mod. If a mod is enabled, it can only be disabled if the things that are dependent on it are never used. The fields added by a mod in the editor should be coloured differently to make it obvious that it is a mod requirement (different colour for each mod)

In this tab, each mod should have a short summary of the things that it adds and how it can be useful for level creators.

Should also make sure to credit the original authors in this section.

* **AdvancedWardenObjective** (AWO)
  * [10k event IDs](https://github.com/hirnukuono/AdvancedWardenObjective/wiki/AWO-Events-(10k))
  * [20k event IDs](https://github.com/hirnukuono/AdvancedWardenObjective/wiki/AWO-Events-(20k))
* **ExcellentObjectiveSetup** (EOS)
  * [Wiki](https://github.com/Amorously/ExcellentObjectiveSetup/wiki)
* **AdditionalRundownAdvancements** (ARA)
  * Triggers should be added as a drag-and-drop system, with selections for triggerbox shapes and customizable sizes. The problem is that these placements are a 3d position, so we need to figure out an intuitive way to actually implement this, especially in tiles with overlapping levels.
  * Maybe just ignore the animation stuff?
  * [Wiki](https://github.com/Amorously/AdditionalRundownAdvancements/wiki)
* **ExtraChainedPuzzleCustomization** (ECPC)
  * Custom T-Scan paths should be implemented as a drag-and-drop node system, same problems as ARA support.
  * [Wiki](https://github.com/Inas-07/ExtraChainedPuzzleCustomization/wiki)

*If we are wanting to add support for modded enemies and weapons, then:*

* **ExtraEnemyCustomization** (EEC)
  * [Wiki Jsonc Files](https://github.com/GTFO-Modding/ExtraEnemyCustomization/tree/master/wikifiles/EEC_1.0)
* **ExtraWeaponCustomization** (EWC)
  * [Wiki](https://github.com/Dinorush/ExtraWeaponCustomization/wiki)