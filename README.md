## OSBExtract

A fork from the original [OSBExtract](https://github.com/nickworonekin/OSBExtract) by [Nick Woronekin](https://github.com/nickworonekin)

A tool for extracting OSB archives from Dreamcast games. Can also extract OSBs from MLT files, and convert P04 and P16 files to WAV.

## Usage

`OSBExtract <file or directory>`

## Differences
 - 

## Game Support List:

 - Puyo Puyo~n - 100%
 
## Games With Issues:
 - Most games
     - Extracted audio isn't the correct speed
 - Illbleed 
 - Seaman
     - Some audio is handled incorrectly.
 - Jet Set Radio 
     - Some audio is handled incorrectly.

## To Do:

 - Figure out proper parsing of MLT header data
 - Figure out unknown ADPCM flag meanings
 - Possibly use [ADPCM by andyroodee](https://github.com/andyroodee/ADPCM) for conversion instead of builtin.
 - See if possible enchancements/documentation exist to improve current codebase in the [OSBExtractJSR](https://github.com/LTSophia/OSBExtractJSR) fork.