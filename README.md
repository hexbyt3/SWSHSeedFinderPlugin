# Gen8 Seed Finder Plugin for PKHeX

A seed finding plugin for PKHeX that helps you search for specific Generation 8 Max Raid encounters in Pokémon Sword & Shield.

## About

This plugin started as part of my custom [ALM](https://github.com/hexbyt3/ALM4SysBot)(AutoLegalityMod) project and has been extracted into a standalone tool for users who just need seed finding functionality. It searches through raid seeds to find encounters that match your exact criteria - whether you're looking for a specific shiny, perfect IVs, or a particular nature.

The tool supports all raid types including Normal Raid Dens, Rare (Crystal) Dens, Event Distributions, and Max Lair encounters from the Crown Tundra DLC.

**Author:** [@hexbyt3](https://github.com/hexbyt3)

## Screenshots
<img width="824" height="548" alt="image" src="https://github.com/user-attachments/assets/72d3ffa3-589c-4f86-9efb-bb45a0015e83" />
<img width="303" height="311" alt="image" src="https://github.com/user-attachments/assets/5ce8082c-101e-4ad3-a0b0-f713fea72896" />


## Features

- Search through millions of seeds with customizable ranges (64-bit seed support)
- Support for all Gen 8 raid types:
  - Normal Raid Dens (version-specific encounters)
  - Rare Crystal Dens
  - Event Distribution raids
  - Max Lair encounters (Crown Tundra)
- Real-time species filtering with type-ahead search
- Detailed search criteria:
  - IV ranges for all stats
  - Nature selection
  - Ability preferences (including Hidden Ability)
  - Gender requirements
  - Shiny status (including square/star specific)
  - Gigantamax capability filtering
- Star rating display (flawless IV count)
- Export results to CSV for spreadsheet analysis
- Dark theme compatible with highlighted shiny results (blue for square, gold for star)
- Direct loading of results into PKHeX editor

## Requirements

- PKHeX (latest version recommended)
- Windows 10 or 11
- .NET 9.0 runtime (usually already installed if you can run PKHeX)

## Installation

1. Download the latest `Gen8SeedFinderPlugin.dll` from the releases page
2. Place it in PKHeX's `plugins` folder (create the folder if it doesn't exist)
3. Restart PKHeX
4. You'll find "Gen 8 Raid Seed Finder" in the Tools menu

## Usage

### Basic Search
1. Open the plugin from Tools > Gen 8 Raid Seed Finder
2. Search or select your target Pokémon species
3. Choose the form if applicable
4. Select which encounter sources to search (Normal Dens, Crystal, Events, Max Lair)
5. Set your search criteria
6. Click Search

### Understanding Raid Mechanics

Different raid types have different characteristics:
- **Normal Dens**: Star rating determines flawless IV count (1★=1IV, 2★=2IV, etc.)
- **Crystal Dens**: Always have higher flawless IV counts
- **Event Distributions**: May have special moves or fixed properties
- **Max Lair**: Always 4 flawless IVs, special shiny mechanics

### Search Tips

- Use the species search box to quickly filter through the Pokémon list
- The plugin shows which sources have encounters for your selected Pokémon
- Hex seed ranges are supported (e.g., 0000000000000000 to 00000000FFFFFFFF)
- Use parallel processing for faster searches - processes ~50,000+ seeds per second
- Double-click any result to load it directly into PKHeX

### Performance

The search uses parallel processing to maximize speed. On modern CPUs:
- Processes approximately 50,000-100,000 seeds per second
- Searching large ranges is feasible but may take time
- Use the Stop button to cancel long searches
- Results are added in real-time as they're found

## Building from Source

If you want to build the plugin yourself:

1. Clone this repository
2. Open `Gen8SeedFinderPlugin.sln` in Visual Studio 2022 or later
3. Ensure you have the PKHeX.Core NuGet package referenced
4. Build in Release mode
5. The compiled DLL will be in `bin/Release/net9.0-windows/`

## Technical Details

This plugin uses PKHeX.Core's Generation 8 raid generation system with:
- **Xoroshiro128+** RNG for raid generation
- Proper seed correlation validation
- Support for all Gen 8 encounter types (EncounterStatic8N, 8NC, 8ND, 8U)
- Full compatibility with overworld correlation requirements
- Accurate star rating and flawless IV generation

The search algorithm:
- Validates seeds against specific encounter constraints
- Generates full Pokémon data to verify all properties
- Ensures proper 64-bit seed correlation
- Handles version-exclusive encounters correctly

## Credits

This plugin wouldn't exist without the incredible work of the PKHeX team:
- **Kurt (@kwsch)** - For creating and maintaining PKHeX, and for the comprehensive encounter database that makes this possible
- **SciresM** - For the initial Gen 8 raid RNG research
- **Leanny** - For raid den location data and encounter tables
- All PKHeX contributors who have helped build the foundation this relies on

Special thanks to the RNG research community for documenting the Xoroshiro128+ algorithm used in Generation 8 raids.

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Contributing

Issues and pull requests are welcome. If you find a bug or have a feature request, please check existing issues first.

## Disclaimer

This tool is for educational purposes. Please respect the game and other players when using modifications.

---

For more information about PKHeX, visit the [official repository](https://github.com/kwsch/PKHeX).
