# Welcome
Welcome to the **GTFO GUI Level Editor**, developed for building custom rundowns and level layouts.

This page serves as a user documentation guide. Assistance for how to use the app, as well as general tips for rundown creation, can be found through this tab.

**Shortcuts & Navigation**

| Shortcut | Action | Description |
| --- | --- | --- |
| `Ctrl+N` | New Rundown | Creates a fresh rundown project with default templates. |
| `Ctrl+S` | Save Rundown | Saves the current rundown project to disk. |
| `Ctrl+O` | Open Rundown | Opens an existing `.rundown` project file. |
| `Ctrl+E` | Quick Export | Exports all datablocks directly to the last export directory. |

The label of almost every field in the editor can be hovered to show a short description of what the field does, and when relevant, also states common default values.

# Level Layout
## Introduction
The left side of the UI is where the user can manage the level layout of their rundown.
This section is divided into four sections:

**Level** is the bar at the top that can be used to switch between different expeditions, and edit settings such as the level's Elevator Geo, Tileset, and Fog settings.

**Dimension** is the second bar from the top, which allows the user to switch between different dimensions, and change the starting geo for each dimension.

**Sectors** is the third bar from the top, which allows the user to edit many different settings for each sector. Most importantly, this is where users edit **the objective**. This is also where users can choose the location of `Bulkhead Keys`.

**Level Layout** takes up the bulk of the left side of the view, and is where users can edit their level's layout, place alarms, spawn enemies, add resources, and much more. Each dimension has its own layout.

`Note for experienced datablock developers: In this editor, sectors are no longer treated as separate level layouts, and all sectors are instead edited alongside each other in a single view.`

## Glossary

| Term | Description |
| --- | --- |
| Datablocks | Datablocks are `json` files used by the game to define almost everything. Entire rundowns are defined purely through extremely complicated datablocks. The purpose of this editor is to generate these datablocks automatically to alleviate the massive learning curve that comes with rundown development for this game.|
| Geomorph | Geomorphs, also known as geos or tiles, are the preset area layouts that the game uses. Most geomorphs are limited to one space in the level layout grid, although some geos can be 1x2 or 2x2. |
| Tileset | Tilesets define a collection of geomorphs that are available for use in a level. The game is split into three tilesets: Mining, Tech, and Service. Each of these have sub-categories, although geos from any subcategory can be used when the parent tileset is selected. |
| Dimensions | A level can consist of multiple dimensions. These effectively act as their own level layouts that players can be transported to/from via `Warp` events. |
| Snatcher Boxes | The most common use for additional dimensions in an expedition is Snatcher Boxes. These are boxes that are generated as a separate dimension, where players get transported to when taken by a Snatcher. Without Snatcher Boxes being added to an expedition, snatchers do not work as intended. |
| Markers | Geomorphs define the overall layout and shape of a tile, while markers fill in the blanks. Bridges, objects, boxes, terminals, etc, are all placed via Markers. In some tiles, markers can completely change the flow of a tile. These are randomly generated based on the Marker seed offset. |
| Plugs | Plugs define the asset that is placed at the edge of a geomorph to connect with other geomorphs. These can include simple doors with surrounding structure, staircases between altitudes, and open room dividers with no doors. |

## Tiles, Rooms & Zones
The connectors at the edge of a geomorph can be clicked by the user to create a new tile for the expedition. This prompts the user to select a geomorph to place, alongside a rotation and altitude.

Most geomorphs are divided into many rooms. When a geomorph is first placed, only the first room is generated. The user can click on the other doors in the room to expand the tile into additional rooms. The in-game name of this room (Room A, B, etc) is determined by the order that these rooms are generated in - the first room in a zone is always A, and the second room that gets expanded to will always be B, etc.

When expanding any door, the user will be prompted to select a door type to use. It is important to note that a regular Room door is not guaranteed to actually spawn a door when connecting two rooms, and may instead opt to use an open room divider between them.
When expanding with any type of security door (Security, Main, Secondary, or Overload door), the other side of the door is now a new zone. A tile is able to have a security door anywhere inside it, and all paths around the door will be automatically blocked off. When expanding rooms, the new room will inherit the same zone as the room being expanded from.

Clicking on any room in a tile will open the settings config for that room. From here, you can swap tabs to edit the Room, Zone, or Geomorph.

The **Room Config** allows users to view basic information about the room, as well as delete it. Note that if a room, tile, or zone is no longer accessible after this tile is deleted, it will also be deleted.

The **Zone Config** allows users to edit a lot of information about what will spawn in this zone, and how the zone should look. This includes the zone lighting, resource kits, HSUs and other clusters, big pickups such as Fog Turbines and Cells, Terminals, Disinfect Stations, DCs (Door Controllers), and more.

The **Geomorph Config** allows users to view the actual geomorph being used, and change its altitude. The geomorph cannot be modified without deleting and re-placing it.

## Sectors & Settings
Each sector has its own settings, which can be accessed by clicking on the sector in the `Sector` bar above the level layout

To make a sector locked via a DC, a DC must be placed in the zone where the sector builds from.
This can be achieved by accessing the Zone Config by clicking on any room in the zone and swapping to the Zone tab, and checking the `Zone Contains DC` box. Note that DCs can only open sectors or Main doors that expand from the same room as them.

To place bulkhead keys, candidate zones for each key can be created by opening the Sector Config by clicking on the sector above the level layout view.

# Objectives
Objectives are per-sector components that define what the players must do to beat each sector in an expedition.

# Alarms & Enemy Waves
## Alarms
TODO

## Enemy Wave Settings
TODO

## Event Alarms
TODO

# Loo

# Environment
TODO

## Lighting
TODO

## Fog
TODO

# Events
## Introduction
TODO

## Door Events
TODO

# Sounds
TODO

# Exporting & Testing
TODO

## Datablocks & Configs
TODO

## Publishing a Rundown
TODO