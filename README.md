# AudicaCameraScript
 A mod for Audica that will load camera cues from a file to control the camera when in a song using MIDI ticks.

This mod is currently a WIP, may not work as intended.

Once you boot the game, it will create a folder in /Audica/Mods/Config/CameraScript where you can place files with name songid.json that contains camera cues.

Uplon launching a song that has a camera cues file the game will move the camera according to the data inside, it will basically move to the location precised in the cue, starting at the tick and ending after tickLength, length basically controls how long it takes to get there.

Rotation currently doesn't work, only position.
