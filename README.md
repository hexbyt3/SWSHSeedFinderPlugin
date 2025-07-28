# SV Seed Finder Plugin for PKHeX

A seed finding plugin for PKHeX that helps you search for specific Generation 9 Tera Raid encounters in Pokemon Scarlet & Violet.

## About

This plugin started as part of my custom [ALM](https://github.com/hexbyt3/ALM4SysBot)(AutoLegalityMod) project and has been extracted into a standalone tool for users who just need seed finding functionality. It searches through raid seeds to find encounters that match your exact criteria - whether you're looking for a specific shiny, perfect IVs, or a particular nature.

The tool supports all raid types including standard Tera Raids, event distributions, and 7-star Unrivaled raids.

**Author:** [@hexbyt3](https://github.com/hexbyt3)

## Screenshots
<img width="955" height="683" alt="image" src="https://github.com/user-attachments/assets/ab139f22-ce0d-48ec-bfb7-6ab7b9e03f5c" />

<img width="400" height="412" alt="image" src="https://github.com/user-attachments/assets/2024ccca-a032-4f20-bdce-164f09cd94e7" />

## Features

- Search through millions of seeds with customizable ranges
- Support for all Gen 9 raid types (Paldea, Kitakami, Blueberry, Events, and 7-star raids)
- Real-time validation that prevents impossible search combinations
- Quick species search with type-ahead filtering
- Detailed search criteria:
  - IV ranges for all stats
  - Nature selection
  - Ability preferences
  - Gender requirements
  - Shiny status (including square/star specific)
- Automatic Met Date correction for 7-star Mighty raids
- Export results to CSV for spreadsheet analysis
- Dark theme compatible with highlighted shiny results

## Requirements

- PKHeX (latest version recommended)
- Windows 10 or 11
- .NET 9.0 runtime (usually already installed if you can run PKHeX)

## Installation

1. Download the latest `SVSeedFinderPlugin.dll` from the releases page
2. Place it in PKHeX's `plugins` folder (create the folder if it doesn't exist)
3. Restart PKHeX
4. You'll find "SV Seed Finder" in the Tools menu

## Usage

### Basic Search
1. Open the plugin from Tools > SV Seed Finder
2. Select your target Pokemon species
3. Choose the form if applicable
4. Set your search criteria
5. Click Search

### Understanding Encounter Constraints

Some encounters have fixed properties that can't be changed. The plugin will highlight incompatible criteria in red and show warnings in the status bar. For example:
- 7-star raids always have 6 perfect IVs
- Event raids might have fixed natures or abilities
- Some raids are shiny-locked

### Search Tips

- Start with broader criteria and narrow down
- Use specific seed ranges if you're checking a particular set
- The "Any Encounter" option searches all possible raids for that species
- Double-click any result to load it directly into PKHeX to quickly export it as a file or load onto your switch via LiveHex.

### Performance

The search processes approximately 10,000 seeds per second. Searching the entire seed range (0x00000000 to 0xFFFFFFFF) would take several days, so it's recommended to:
- Set reasonable result limits
- Use specific seed ranges when possible
- Stop searches once you find suitable results

## Building from Source

If you want to build the plugin yourself:

1. Clone this repository
2. Open `SVSeedFinderPlugin.sln` in Visual Studio 2022 or later
4. Build in Release mode
5. The compiled DLL will be in `bin/Release/net9.0-windows/`

## Technical Details

This plugin uses PKHeX.Core's raid generation system to ensure all found seeds produce legal Pokemon. The search algorithm:
- Validates each seed against the selected encounter's constraints
- Generates the full Pokemon data to check all properties
- Ensures proper correlation between seed and Pokemon data

## Credits

This plugin wouldn't exist without the incredible work of the PKHeX team:
- **Kurt (@kwsch)** - For creating and maintaining PKHeX, and for the comprehensive encounter database that makes this possible
- **Manu (@manu098vm)** - For the raid seed research and RNG implementation that powers the search functionality
- All PKHeX contributors who have helped build the foundation this relies on

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Contributing

Issues and pull requests are welcome. If you find a bug or have a feature request, please check existing issues first.

## Disclaimer

This tool is for educational purposes. Please respect the game and other players when using modifications.

---

For more information about PKHeX, visit the [official repository](https://github.com/kwsch/PKHeX).
