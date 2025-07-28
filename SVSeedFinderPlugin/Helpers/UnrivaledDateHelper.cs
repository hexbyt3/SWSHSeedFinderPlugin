using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;

namespace SVSeedFinderPlugin.Helpers;

/// <summary>
/// Helper class for handling Unrivaled (7-star) Tera Raid event dates and validation
/// </summary>
public static class UnrivaledDateHelper
{
    /// <summary>
    /// Contains the valid date ranges for 7-star "Unrivaled" Tera Raid events where Pokémon could receive the Mightiest Mark.
    /// </summary>
    /// <remarks>
    /// Each entry maps a species ID to a list of date ranges representing the distribution windows for that species'
    /// 7-star raid events. These dates are based on official event schedules and are used to ensure legal Met Dates
    /// for Pokémon with the Mightiest Mark ribbon.
    /// </remarks>
    private static readonly Dictionary<int, List<(DateOnly Start, DateOnly End)>> UnrivaledDateRanges = new()
    {
        // Generation 1
        [(int)Species.Charizard] = [(new(2022, 12, 02), new(2022, 12, 04)), (new(2022, 12, 16), new(2022, 12, 18)), (new(2024, 03, 13), new(2024, 03, 17))], // Charizard
        [(int)Species.Venusaur] = [(new(2024, 02, 28), new(2024, 03, 05))], // Venusaur
        [(int)Species.Blastoise] = [(new(2024, 03, 06), new(2024, 03, 12))], // Blastoise

        // Generation 2
        [(int)Species.Meganium] = [(new(2024, 04, 05), new(2024, 04, 07)), (new(2024, 04, 12), new(2024, 04, 14))], // Meganium
        [(int)Species.Typhlosion] = [(new(2023, 04, 14), new(2023, 04, 16)), (new(2023, 04, 21), new(2023, 04, 23))], // Typhlosion
        [(int)Species.Feraligatr] = [(new(2024, 11, 01), new(2024, 11, 03)), (new(2024, 11, 08), new(2024, 11, 10))], // Feraligatr
        [(int)Species.Porygon2] = [(new(2025, 06, 05), new(2025, 06, 15))], // Porygon2

        // Generation 3
        [(int)Species.Sceptile] = [(new(2024, 06, 28), new(2024, 06, 30)), (new(2024, 07, 05), new(2024, 07, 07))], // Sceptile
        [(int)Species.Blaziken] = [(new(2024, 01, 12), new(2024, 01, 14)), (new(2024, 01, 19), new(2024, 01, 21))], // Blaziken
        [(int)Species.Swampert] = [(new(2024, 05, 31), new(2024, 06, 02)), (new(2024, 06, 07), new(2024, 06, 09))], // Swampert

        // Generation 4
        [(int)Species.Empoleon] = [(new(2024, 02, 02), new(2024, 02, 04)), (new(2024, 02, 09), new(2024, 02, 11))], // Empoleon
        [(int)Species.Infernape] = [(new(2024, 10, 04), new(2024, 10, 06)), (new(2024, 10, 11), new(2024, 10, 13))],  // Infernape
        [(int)Species.Torterra] = [(new(2024, 11, 15), new(2024, 11, 17)), (new(2024, 11, 22), new(2024, 11, 24))],  // Torterra

        // Generation 5
        [(int)Species.Emboar] = [(new(2024, 06, 14), new(2024, 06, 16)), (new(2024, 06, 21), new(2024, 06, 23))], // Emboar
        [(int)Species.Serperior] = [(new(2024, 09, 20), new(2024, 09, 22)), (new(2024, 09, 27), new(2024, 09, 29))], // Serperior

        // Generation 6
        [(int)Species.Chesnaught] = [(new(2023, 05, 12), new(2023, 05, 14)), (new(2023, 06, 16), new(2023, 06, 18))], // Chesnaught
        [(int)Species.Delphox] = [(new(2023, 07, 07), new(2023, 07, 09)), (new(2023, 07, 14), new(2023, 07, 16))], // Delphox

        // Generation 7
        [(int)Species.Decidueye] = [(new(2023, 03, 17), new(2023, 03, 19)), (new(2023, 03, 24), new(2023, 03, 26))], // Decidueye
        [(int)Species.Primarina] = [(new(2024, 05, 10), new(2024, 05, 12)), (new(2024, 05, 17), new(2024, 05, 19))], // Primarina
        [(int)Species.Incineroar] = [(new(2024, 09, 06), new(2024, 09, 08)), (new(2024, 09, 13), new(2024, 09, 15))], // Incineroar

        // Generation 8
        [(int)Species.Rillaboom] = [(new(2023, 07, 28), new(2023, 07, 30)), (new(2023, 08, 04), new(2023, 08, 06))], // Rillaboom
        [(int)Species.Cinderace] = [(new(2022, 12, 30), new(2023, 01, 01)), (new(2023, 01, 13), new(2023, 01, 15))], // Cinderace
        [(int)Species.Inteleon] = [(new(2023, 04, 28), new(2023, 04, 30)), (new(2023, 05, 05), new(2023, 05, 07))], // Inteleon

        // Others
        [(int)Species.Pikachu] = [(new(2023, 02, 24), new(2023, 02, 27)), (new(2024, 07, 12), new(2024, 07, 25))], // Pikachu
        [(int)Species.Eevee] = [(new(2023, 11, 17), new(2023, 11, 20))], // Eevee
        [(int)Species.Mewtwo] = [(new(2023, 09, 01), new(2023, 09, 17))], // Mewtwo
        [(int)Species.Greninja] = [(new(2023, 01, 27), new(2023, 01, 29)), (new(2023, 02, 10), new(2023, 02, 12))], // Greninja
        [(int)Species.Samurott] = [(new(2023, 03, 31), new(2023, 04, 02)), (new(2023, 04, 07), new(2023, 04, 09))], // Samurott
        [(int)Species.IronBundle] = [(new(2023, 12, 22), new(2023, 12, 24))], // Iron Bundle
        [(int)Species.Dondozo] = [(new(2024, 07, 26), new(2024, 08, 08))], // Dondozo
        [(int)Species.Dragonite] = [(new(2024, 08, 23), new(2024, 09, 01))], // Dragonite
        [(int)Species.Meowscarada] = [(new(2025, 02, 28), new(2025, 03, 06))], // Meowscarada
        [(int)Species.Skeledirge] = [(new(2025, 03, 06), new(2025, 03, 13))], // Skeledirge
        [(int)Species.Quaquaval] = [(new(2025, 03, 14), new(2025, 03, 20))], // Quaquaval
        [(int)Species.Tyranitar] = [(new(2025, 03, 28), new(2025, 03, 30)), (new(2025, 04, 04), new(2025, 04, 06))], // Tyranitar
        [(int)Species.Salamence] = [(new(2025, 04, 18), new(2025, 04, 20)), (new(2025, 04, 25), new(2025, 04, 27))], // Salamence
        [(int)Species.Metagross] = [(new(2025, 05, 09), new(2025, 05, 11)), (new(2025, 05, 12), new(2025, 05, 19))], // Metagross
        [(int)Species.Garchomp] = [(new(2025, 05, 22), new(2025, 05, 25)), (new(2025, 05, 30), new(2025, 06, 01))], // Garchomp
        [(int)Species.Baxcalibur] = [(new(2025, 06, 19), new(2025, 06, 22)), (new(2025, 06, 27), new(2025, 06, 29))], // Baxcalibur
        [(int)Species.Kommoo] = [(new(2025, 07, 11), new(2025, 07, 13)), (new(2025, 07, 18), new(2025, 07, 20))], // Kommo-o
    };

    /// <summary>
    /// Checks and sets a valid Met Date for Pokémon with the Mightiest Mark ribbon from 7-star Tera Raid events.
    /// </summary>
    /// <param name="pk">The PKM to check and potentially modify the Met Date for.</param>
    /// <remarks>
    /// This method ensures that Pokémon obtained from 7-star "Unrivaled" raids have Met Dates that match
    /// the actual event distribution windows. If the current Met Date is invalid or missing, a random valid
    /// date from the available event windows will be assigned.
    /// </remarks>
    public static void CheckAndSetUnrivaledDate(this PKM pk)
    {
        if (pk is not IRibbonSetMark9 ribbonSetMark || !ribbonSetMark.RibbonMarkMightiest)
            return;

        List<(DateOnly Start, DateOnly End)> dateRanges;

        if (UnrivaledDateRanges.TryGetValue(pk.Species, out var ranges))
        {
            dateRanges = ranges;
        }
        else if (pk.Species is (int)Species.Decidueye or (int)Species.Typhlosion or (int)Species.Samurott && pk.Form == 1)
        {
            // Special handling for Hisuian forms
            dateRanges = pk.Species switch
            {
                (int)Species.Decidueye => [(new(2023, 10, 06), new(2023, 10, 08)), (new(2023, 10, 13), new(2023, 10, 15))],
                (int)Species.Typhlosion => [(new(2023, 11, 03), new(2023, 11, 05)), (new(2023, 11, 10), new(2023, 11, 12))],
                (int)Species.Samurott => [(new(2023, 11, 24), new(2023, 11, 26)), (new(2023, 12, 01), new(2023, 12, 03))],
                _ => []
            };
        }
        else
        {
            return;
        }

        if (!pk.MetDate.HasValue || !IsDateInRanges(pk.MetDate.Value, dateRanges))
        {
            SetRandomDateFromRanges(pk, dateRanges);
        }
    }

    /// <summary>
    /// Determines if a given date falls within any of the provided date ranges.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="ranges">A list of date ranges to check against.</param>
    /// <returns><c>true</c> if the date falls within any of the ranges; otherwise, <c>false</c>.</returns>
    private static bool IsDateInRanges(DateOnly date, List<(DateOnly Start, DateOnly End)> ranges)
    {
        return ranges.Any(range => date >= range.Start && date <= range.End);
    }

    /// <summary>
    /// Sets a random Met Date for the PKM from one of the provided valid date ranges.
    /// </summary>
    /// <param name="pk">The PKM to set the Met Date for.</param>
    /// <param name="ranges">A list of valid date ranges to choose from.</param>
    /// <remarks>
    /// This method randomly selects one of the date ranges and then randomly selects a date within
    /// that range (inclusive of both start and end dates). If no ranges are provided, the method returns
    /// without making any changes.
    /// </remarks>
    private static void SetRandomDateFromRanges(PKM pk, List<(DateOnly Start, DateOnly End)> ranges)
    {
        if (ranges.Count == 0)
            return;

        var random = new Random();
        var (Start, End) = ranges[random.Next(ranges.Count)];
        var days = (End.DayNumber - Start.DayNumber) + 1;
        var randomDays = random.Next(days);
        pk.MetDate = Start.AddDays(randomDays);
    }

    /// <summary>
    /// Gets the required gender for species that appear in Mighty raids.
    /// </summary>
    /// <param name="species">The species to check</param>
    /// <param name="form">The form to check (for regional variants)</param>
    /// <returns>The required gender for Mighty raids, or null if not gender-locked</returns>
    public static byte? GetMightyRaidGender(ushort species, byte form = 0) => (Species)species switch
    {
        // Gen 1
        Species.Charizard => 0,      // Male only
        Species.Venusaur => 0,       // Male only
        Species.Blastoise => 0,      // Male only
        Species.Pikachu => 0,        // Male only
        Species.Mewtwo => 2,         // Genderless (always)
        Species.Eevee => 0,          // Male only
        Species.Dragonite => 0,      // Male only

        // Gen 2
        Species.Meganium => 1,       // Female only
        Species.Typhlosion when form == 0 => 0,  // Male only (Johtonian)
        Species.Typhlosion when form == 1 => 1,  // Female only (Hisuian)
        Species.Feraligatr => 0,     // Male only
        Species.Porygon2 => 2,       // Genderless (always)
        Species.Tyranitar => 0,      // Male only

        // Gen 3
        Species.Sceptile => 0,       // Male only
        Species.Blaziken => 0,       // Male only
        Species.Swampert => 0,       // Male only
        Species.Salamence => 0,      // Male only
        Species.Metagross => 2,      // Genderless (always)

        // Gen 4
        Species.Torterra => 0,       // Male only
        Species.Infernape => 0,      // Male only
        Species.Empoleon => 0,       // Male only
        Species.Garchomp => 0,       // Male only

        // Gen 5
        Species.Serperior => 0,      // Male only
        Species.Emboar => 0,         // Male only
        Species.Samurott when form == 0 => 0,  // Male only (Unovan)
        Species.Samurott when form == 1 => 0,  // Male only (Hisuian)

        // Gen 6
        Species.Chesnaught => 0,     // Male only
        Species.Delphox => 1,        // Female only
        Species.Greninja => 0,       // Male only

        // Gen 7
        Species.Decidueye when form == 0 => 0,  // Male only (Alolan)
        Species.Decidueye when form == 1 => 0,  // Male only (Hisuian)
        Species.Incineroar => 0,     // Male only
        Species.Primarina => 1,      // Female only

        // Gen 8
        Species.Rillaboom => 0,      // Male only
        Species.Cinderace => 0,      // Male only
        Species.Inteleon => 0,       // Male only
        Species.Dondozo => 0,        // Male only

        // Gen 9
        Species.Meowscarada => 1,    // Female only
        Species.Skeledirge => 0,     // Male only
        Species.Quaquaval => 0,      // Male only
        Species.Baxcalibur => 0,     // Male only
        Species.IronBundle => 2,     // Genderless (always)
        Species.Kommoo => 0,         // Male only

        _ => null
    };
}
