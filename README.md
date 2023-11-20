## OSBExtract
A fork from the original [OSBExtract](https://github.com/nickworonekin/OSBExtract) by [Nick Woronekin](https://github.com/nickworonekin)

A tool for extracting OSB archives from Dreamcast games. Can also extract OSBs from MLT files, and convert P04 and P16 files to WAV.

## Usage
```
OSBExtract -i <file or directory> 

  -i, --input      Required. Input files to be processed. Can be files or
                   directories.
  Optional:
  -o, --output     WAVE output folder (Default is same as input folder).
  -x, --extra      Output extra data not included in the OSB to WAVE conversion (Saves to output folder).
  -v, --verbose    Set view verbose information used for debugging.
  -s, --sample     Use alternate sample rate of 32kHz (Default is 44.1kHz). Appends "(32kHz)" to filename
  --help           Display this help screen.
  --version        Display version information.
 ```

## Differences
- OSB files now have their data extracted in a dynamic way.
- AMKR, NITS, PALP, EMUN, IPTC, RPCE, and ETTX metadata are successfully skipped during extraction.
- PCM class has been completely rewritten to allow customized attributes if needed. 
    - This will be useful in case OSB data is found to have different encodings, such as a different sample rate.
- [AicaADPCM2WAV by Sappharad](https://github.com/Sappharad/AicaADPCM2WAV) is now used to convert ADPCM data.
- Uses [`commandline`](https://github.com/commandlineparser/commandline) package to assist in handling command line argument options.

## Game Support List:
- Puyo Puyo~n - 100%
 
## Games With Issues:
- Most games (Illbleed, Seaman, Jet Set Radio for examples)
    - Extracted audio isn't always the correct sample rate

## To Do:
- Figure out proper parsing of MLT header data
- Figure out unknown ADPCM flag meanings
- Figure out how sample rate is correctly determined