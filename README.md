## OSBExtract
A fork from the original [OSBExtract](https://github.com/nickworonekin/OSBExtract) by [Nick Woronekin](https://github.com/nickworonekin)

A tool for extracting OSB archives from Dreamcast games. Can also extract OSBs from MLT files, and convert P04 and P16 files to WAV.

## Usage
`OSBExtract <file or directory>`

## Differences
- OSB files now have their data extracted in a dnyamic way.
- AMKR, NITS, PALP, EMUN, IPTC, RPCE, and ETTX metadata are successfully skipped during extraction.
- PCM class has been completely rewritten to allow customized attributes if needed. 
    - This will be useful in case OSB data is found to have different encodings, such as a different sample rate.

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