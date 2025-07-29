using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;
using SWSHSeedFinderPlugin.Helpers;

namespace SWSHSeedFinderPlugin.GUI;

/// <summary>
/// Gen 8 Seed Finder Plugin for PKHeX
/// Provides comprehensive seed finding capabilities for Generation 8 raid encounters.
/// Author: hexbyt3
/// </summary>
public partial class Gen8SeedFinderForm : Form
{
    private readonly ISaveFileProvider _saveFileEditor;
    private readonly IPKMView _pkmEditor;
    private CancellationTokenSource? _searchCts;
    private List<SeedResult> _results = [];
    private List<EncounterWrapper> _cachedEncounters = [];
    private EncounterSource _availableSources;
    private List<ComboItem> _allSpecies = [];
    private readonly Lock _resultsLock = new();

    // Preview panel components
    private Panel _previewPanel = null!;
    private PictureBox _previewSprite = null!;
    private Label _previewTitle = null!;
    private Label _previewDetails = null!;
    private Label _previewStats = null!;
    private Label _previewMoves = null!;
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Flags for different encounter sources in Generation 8.
    /// </summary>
    [Flags]
    private enum EncounterSource
    {
        None = 0,
        Normal = 1 << 0,
        Crystal = 1 << 1,
        Distribution = 1 << 2,
        Underground = 1 << 3,
    }

    /// <summary>
    /// Initializes a new instance of the Gen8SeedFinderForm.
    /// </summary>
    /// <param name="saveFileEditor">Save file provider interface</param>
    /// <param name="pkmEditor">PKM editor interface</param>
    public Gen8SeedFinderForm(ISaveFileProvider saveFileEditor, IPKMView pkmEditor)
    {
        _saveFileEditor = saveFileEditor;
        _pkmEditor = pkmEditor;
        InitializeComponent();
        InitializePreviewPanel();
        LoadSpeciesList();
        LoadTrainerData();
        SetupEventHandlers();
    }

    /// <summary>
    /// Initializes the preview panel components.
    /// </summary>
    private void InitializePreviewPanel()
    {
        _previewPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 200,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Control,
            Visible = false
        };

        _previewSprite = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(68, 56),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            BorderStyle = BorderStyle.FixedSingle
        };

        _previewTitle = new Label
        {
            Location = new Point(85, 10),
            Size = new Size(250, 20),
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
        };

        _previewDetails = new Label
        {
            Location = new Point(85, 35),
            Size = new Size(250, 60),
            AutoSize = false
        };

        _previewStats = new Label
        {
            Location = new Point(340, 10),
            Size = new Size(180, 180),
            AutoSize = false,
            Font = new Font("Consolas", 9)
        };

        _previewMoves = new Label
        {
            Location = new Point(85, 100),
            Size = new Size(250, 90),
            AutoSize = false
        };

        _previewPanel.Controls.AddRange(new Control[] {
            _previewSprite, _previewTitle, _previewDetails, _previewStats, _previewMoves
        });

        // Add preview panel to results panel
        resultsPanel?.Controls.Add(_previewPanel);

        // Adjust grid size when preview is shown
        if (resultsGrid != null)
        {
            resultsGrid.SelectionChanged += ResultsGrid_SelectionChanged;
        }
    }

    /// <summary>
    /// Handles selection change events in the results grid to update preview.
    /// </summary>
    private void ResultsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (resultsGrid.SelectedRows.Count == 0 || _previewPanel == null)
        {
            if (_previewPanel != null)
                _previewPanel.Visible = false;
            return;
        }

        if (resultsGrid.SelectedRows[0].Tag is SeedResult result)
        {
            UpdatePreviewPanel(result);

            // Adjust grid height to accommodate preview
            if (!_previewPanel.Visible)
            {
                _previewPanel.Visible = true;
                resultsGrid.Height = resultsPanel.Height - _previewPanel.Height;
            }
        }
    }

    /// <summary>
    /// Updates the preview panel with the selected result's information.
    /// </summary>
    /// <param name="result">Selected seed result</param>
    private void UpdatePreviewPanel(SeedResult result)
    {
        if (_previewSprite == null || _previewTitle == null || _previewDetails == null ||
            _previewStats == null || _previewMoves == null)
        {
            return;
        }

        var pk = result.Pokemon;
        var wrapper = new EncounterWrapper(result.Encounter, GameVersion.SWSH);

        // Load sprite asynchronously
        _ = LoadPokemonSpriteAsync(pk);

        // Set title
        var speciesName = GameInfo.Strings.specieslist[pk.Species];
        var formName = pk.Form > 0 ? $" ({FormConverter.GetFormList(pk.Species, GameInfo.Strings.types, GameInfo.Strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen8)[pk.Form]})" : "";
        var shinyIndicator = pk.IsShiny ? (pk.ShinyXor == 0 ? " ■" : " ★") : "";
        _previewTitle.Text = $"{speciesName}{formName}{shinyIndicator}";
        _previewTitle.ForeColor = pk.IsShiny ? (pk.ShinyXor == 0 ? Color.DeepSkyBlue : Color.Gold) : SystemColors.ControlText;

        // Set details
        var details = new List<string>
        {
            $"Seed: {result.Seed:X16}",
            $"Nature: {pk.Nature} | Gender: {GetGenderSymbol(pk.Gender)}",
            $"Ability: {GetAbilityName(pk)} ({GetAbilityType(pk)})",
            pk.CanGigantamax ? "Can Gigantamax" : ""
        };

        details.Add($"Encounter: {wrapper.GetDescription()}");

        _previewDetails.Text = string.Join("\n", details.Where(s => !string.IsNullOrEmpty(s)));

        // Set stats
        var stats = new[]
        {
            "Stats:",
            $"HP:  {pk.IV_HP,2} IV | {pk.Stat_HPMax,3} Total",
            $"Atk: {pk.IV_ATK,2} IV | {pk.Stat_ATK,3} Total",
            $"Def: {pk.IV_DEF,2} IV | {pk.Stat_DEF,3} Total",
            $"SpA: {pk.IV_SPA,2} IV | {pk.Stat_SPA,3} Total",
            $"SpD: {pk.IV_SPD,2} IV | {pk.Stat_SPD,3} Total",
            $"Spe: {pk.IV_SPE,2} IV | {pk.Stat_SPE,3} Total",
            "",
            $"Height: {pk.HeightScalar} | Weight: {pk.WeightScalar}"
        };
        _previewStats.Text = string.Join("\n", stats);

        // Set moves
        var moveNames = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var move = pk.GetMove(i);
            if (move != 0)
            {
                var moveName = GameInfo.Strings.movelist[move];
                moveNames.Add($"• {moveName}");
            }
        }
        _previewMoves.Text = moveNames.Count > 0 ? "Moves:\n" + string.Join("\n", moveNames) : "";
    }

    /// <summary>
    /// Gets the gender symbol for display.
    /// </summary>
    /// <param name="gender">Gender value</param>
    /// <returns>Gender symbol string</returns>
    private static string GetGenderSymbol(int gender) => gender switch
    {
        0 => "♂",
        1 => "♀",
        _ => "-"
    };

    /// <summary>
    /// Gets the ability type description.
    /// </summary>
    /// <param name="pk">Pokémon to check</param>
    /// <returns>Ability type string</returns>
    private static string GetAbilityType(PK8 pk) => pk.AbilityNumber switch
    {
        1 => "Ability 1",
        2 => "Ability 2",
        4 => "Hidden",
        _ => "?"
    };

    /// <summary>
    /// Loads the Pokémon sprite asynchronously from the web.
    /// </summary>
    /// <param name="pk">Pokémon to load sprite for</param>
    private async Task LoadPokemonSpriteAsync(PK8 pk)
    {
        try
        {
            var url = GetPokemonImageUrl(pk);

            // System.Diagnostics.Debug.WriteLine($"Loading sprite: {GameInfo.Strings.specieslist[pk.Species]} (G-Max: {pk.CanGigantamax}) from {url}");

            using var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                var image = Image.FromStream(stream);

                // Update UI on main thread
                if (_previewSprite.InvokeRequired)
                {
                    _previewSprite.Invoke(() => _previewSprite.Image = image);
                }
                else
                {
                    _previewSprite.Image = image;
                }
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine($"Failed to load sprite: HTTP {response.StatusCode} for {url}");
                SetEmptySprite();
            }
        }
        catch (Exception)
        {
            // System.Diagnostics.Debug.WriteLine($"Error loading sprite: {ex.Message}");
            SetEmptySprite();
        }
    }

    /// <summary>
    /// Sets an empty sprite in the preview panel.
    /// </summary>
    private void SetEmptySprite()
    {
        if (_previewSprite.InvokeRequired)
        {
            _previewSprite.Invoke(() => _previewSprite.Image = null);
        }
        else
        {
            _previewSprite.Image = null;
        }
    }

    /// <summary>
    /// Gets the Pokémon image URL for HOME sprites.
    /// </summary>
    /// <param name="pk">Pokémon to get image for</param>
    /// <returns>Image URL string</returns>
    private static string GetPokemonImageUrl(PK8 pk)
    {
        var baseLink = "https://raw.githubusercontent.com/hexbyt3/HomeImages/master/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');

        // Check if can Gigantamax
        var canGmax = pk.CanGigantamax;

        // Determine if we need gender-specific sprites
        bool md = false;
        bool fd = false;

        // Check for gender-dependent species (but NOT if G-Max)
        var genderDependentSpecies = new[]
        {
            (int)Species.Venusaur, (int)Species.Butterfree, (int)Species.Rattata, (int)Species.Raticate,
            (int)Species.Pikachu, (int)Species.Zubat, (int)Species.Golbat, (int)Species.Gloom,
            (int)Species.Vileplume, (int)Species.Kadabra, (int)Species.Alakazam, (int)Species.Doduo,
            (int)Species.Dodrio, (int)Species.Hypno, (int)Species.Goldeen, (int)Species.Seaking,
            (int)Species.Scyther, (int)Species.Magikarp, (int)Species.Gyarados, (int)Species.Eevee,
            (int)Species.Meganium, (int)Species.Ledyba, (int)Species.Ledian, (int)Species.Xatu,
            (int)Species.Sudowoodo, (int)Species.Politoed, (int)Species.Aipom, (int)Species.Wooper,
            (int)Species.Quagsire, (int)Species.Murkrow, (int)Species.Wobbuffet, (int)Species.Girafarig,
            (int)Species.Gligar, (int)Species.Steelix, (int)Species.Scizor, (int)Species.Heracross,
            (int)Species.Sneasel, (int)Species.Ursaring, (int)Species.Piloswine, (int)Species.Octillery,
            (int)Species.Houndoom, (int)Species.Donphan, (int)Species.Torchic, (int)Species.Combusken,
            (int)Species.Blaziken, (int)Species.Beautifly, (int)Species.Dustox, (int)Species.Ludicolo,
            (int)Species.Nuzleaf, (int)Species.Shiftry, (int)Species.Swalot, (int)Species.Camerupt,
            (int)Species.Cacturne, (int)Species.Milotic, (int)Species.Relicanth, (int)Species.Starly,
            (int)Species.Staravia, (int)Species.Staraptor, (int)Species.Bidoof, (int)Species.Bibarel,
            (int)Species.Kricketot, (int)Species.Kricketune, (int)Species.Shinx, (int)Species.Luxio,
            (int)Species.Luxray, (int)Species.Roserade, (int)Species.Combee, (int)Species.Pachirisu,
            (int)Species.Buizel, (int)Species.Floatzel, (int)Species.Ambipom, (int)Species.Gible,
            (int)Species.Gabite, (int)Species.Garchomp, (int)Species.Hippopotas, (int)Species.Hippowdon,
            (int)Species.Croagunk, (int)Species.Toxicroak, (int)Species.Finneon, (int)Species.Lumineon,
            (int)Species.Snover, (int)Species.Abomasnow, (int)Species.Weavile, (int)Species.Rhyperior,
            (int)Species.Tangrowth, (int)Species.Mamoswine, (int)Species.Unfezant, (int)Species.Frillish,
            (int)Species.Jellicent, (int)Species.Pyroar, (int)Species.Meowstic, (int)Species.Indeedee
        };

        if (genderDependentSpecies.Contains(pk.Species) && !canGmax && pk.Form == 0)
        {
            if (pk.Gender == 0 && pk.Species != (int)Species.Torchic)
                md = true;
            else
                fd = true;
        }

        // Special case for Sneasel
        if (pk.Species == (int)Species.Sneasel)
        {
            if (pk.Gender == 0)
                md = true;
            else
                fd = true;
        }

        // Species number formatting
        baseLink[2] = pk.Species < 10 ? $"000{pk.Species}" :
                      pk.Species < 100 ? $"00{pk.Species}" :
                      pk.Species < 1000 ? $"0{pk.Species}" :
                      $"{pk.Species}";

        // Form number formatting with special cases
        int form = pk.Species switch
        {
            (int)Species.Sinistea or (int)Species.Polteageist or (int)Species.Rockruff or (int)Species.Mothim => 0,
            (int)Species.Alcremie when pk.IsShiny || canGmax => 0,
            _ => pk.Form,
        };
        baseLink[3] = form < 10 ? $"00{form}" : $"0{form}";

        // Gender designation
        baseLink[4] = pk.PersonalInfo.OnlyFemale ? "fo" :
                      pk.PersonalInfo.OnlyMale ? "mo" :
                      pk.PersonalInfo.Genderless ? "uk" :
                      fd ? "fd" :
                      md ? "md" :
                      "mf";

        // Gigantamax
        baseLink[5] = canGmax ? "g" : "n";

        // Form argument (for Alcremie, etc.)
        baseLink[6] = pk.Species == (int)Species.Alcremie && !canGmax ?
                      $"0000000{pk.FormArgument}" :
                      "00000000";

        // Shiny status
        baseLink[8] = pk.IsShiny ? "r.png" : "n.png";

        return string.Join("_", baseLink);
    }

    /// <summary>
    /// Sets up event handlers and enables double buffering for the results grid.
    /// </summary>
    private void SetupEventHandlers()
    {
        // Enable double buffering for smoother updates
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, resultsGrid, [true]);
    }

    /// <summary>
    /// Loads trainer data from the current save file.
    /// </summary>
    private void LoadTrainerData()
    {
        var sav = _saveFileEditor.SAV;
        tidNum.Value = sav.TID16;
        sidNum.Value = sav.SID16;
    }

    /// <summary>
    /// Loads the species list for Generation 8 Pokémon.
    /// </summary>
    private void LoadSpeciesList()
    {
        var species = new List<ComboItem>();
        var names = GameInfo.Strings.specieslist;

        for (int i = 1; i < names.Length; i++)
        {
            if (PersonalTable.SWSH.IsPresentInGame((ushort)i, 0))
                species.Add(new ComboItem(names[i], i));
        }

        _allSpecies = species;
        speciesCombo.DisplayMember = "Text";
        speciesCombo.ValueMember = "Value";
        speciesCombo.DataSource = species;
    }

    /// <summary>
    /// Handles the species search box text changed event to filter species list.
    /// </summary>
    private void SpeciesSearchBox_TextChanged(object? sender, EventArgs e)
    {
        var searchText = speciesSearchBox.Text.Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            speciesCombo.DataSource = _allSpecies;
            return;
        }

        var filteredSpecies = _allSpecies.Where(s =>
            s.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filteredSpecies.Count > 0)
        {
            speciesCombo.DataSource = filteredSpecies;
            if (filteredSpecies.Count == 1)
                speciesCombo.SelectedIndex = 0;
        }
        else
        {
            speciesCombo.DataSource = new List<ComboItem>();
        }
    }

    /// <summary>
    /// Handles species selection change event.
    /// </summary>
    private void SpeciesCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (speciesCombo.SelectedValue is not int species)
            return;

        UpdateFormList(species);
        UpdateEncounterList(species);
        UpdateSourceDisplay();
        UpdateEncounterCombo();
    }

    /// <summary>
    /// Updates the form list for the selected species.
    /// </summary>
    /// <param name="species">Species ID</param>
    private void UpdateFormList(int species)
    {
        var forms = FormConverter.GetFormList((ushort)species, GameInfo.Strings.types, GameInfo.Strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen8);

        formCombo.DisplayMember = "Text";
        formCombo.ValueMember = "Value";
        formCombo.DataSource = forms.Select((f, i) => new ComboItem(f, i)).ToList();
    }

    /// <summary>
    /// Updates the encounter list for the selected species based on selected sources.
    /// </summary>
    /// <param name="species">Species ID</param>
    private void UpdateEncounterList(int species)
    {
        var encounters = new List<EncounterWrapper>();
        _availableSources = EncounterSource.None;

        var selectedSources = GetSelectedSources();

        // Get normal raid encounters
        if (selectedSources.HasFlag(EncounterSource.Normal))
        {
            var normalRaidsSW = GetNormalRaidEncounters((ushort)species, GameVersion.SW);
            var normalRaidsSH = GetNormalRaidEncounters((ushort)species, GameVersion.SH);
            encounters.AddRange(normalRaidsSW.Select(e => new EncounterWrapper(e, GameVersion.SW)));
            encounters.AddRange(normalRaidsSH.Select(e => new EncounterWrapper(e, GameVersion.SH)));
            if (normalRaidsSW.Count > 0 || normalRaidsSH.Count > 0)
                _availableSources |= EncounterSource.Normal;
        }

        // Get crystal encounters
        if (selectedSources.HasFlag(EncounterSource.Crystal))
        {
            var crystalRaids = GetCrystalEncounters((ushort)species);
            encounters.AddRange(crystalRaids.Select(e => new EncounterWrapper(e, GameVersion.SWSH)));
            if (crystalRaids.Count > 0)
                _availableSources |= EncounterSource.Crystal;
        }

        // Get distribution encounters
        if (selectedSources.HasFlag(EncounterSource.Distribution))
        {
            var distRaidsSW = GetDistributionEncounters((ushort)species, GameVersion.SW);
            var distRaidsSH = GetDistributionEncounters((ushort)species, GameVersion.SH);
            encounters.AddRange(distRaidsSW.Select(e => new EncounterWrapper(e, GameVersion.SW)));
            encounters.AddRange(distRaidsSH.Select(e => new EncounterWrapper(e, GameVersion.SH)));
            if (distRaidsSW.Count > 0 || distRaidsSH.Count > 0)
                _availableSources |= EncounterSource.Distribution;
        }

        // Get underground encounters
        if (selectedSources.HasFlag(EncounterSource.Underground))
        {
            var undergroundRaids = GetUndergroundEncounters((ushort)species);
            encounters.AddRange(undergroundRaids.Select(e => new EncounterWrapper(e, GameVersion.SWSH)));
            if (undergroundRaids.Count > 0)
                _availableSources |= EncounterSource.Underground;
        }

        _cachedEncounters = encounters;
    }

    /// <summary>
    /// Gets the currently selected encounter sources from checkboxes.
    /// </summary>
    /// <returns>Combined encounter source flags</returns>
    private EncounterSource GetSelectedSources()
    {
        var sources = EncounterSource.None;
        if (normalDensCheck.Checked) sources |= EncounterSource.Normal;
        if (crystalDensCheck.Checked) sources |= EncounterSource.Crystal;
        if (eventDensCheck.Checked) sources |= EncounterSource.Distribution;
        if (maxLairCheck.Checked) sources |= EncounterSource.Underground;
        return sources == EncounterSource.None ? EncounterSource.Normal | EncounterSource.Crystal | EncounterSource.Distribution | EncounterSource.Underground : sources;
    }

    /// <summary>
    /// Updates the encounter combo box with available encounters.
    /// </summary>
    private void UpdateEncounterCombo()
    {
        var items = new List<ComboItem> { new("All Encounters", -1) };

        var form = (byte)(formCombo.SelectedValue as int? ?? 0);
        var groupedEncounters = _cachedEncounters
            .Where(e => e.Form == form || e.Form >= EncounterUtil.FormDynamic)
            .GroupBy(e => e.GetDescription())
            .Select((g, i) => new { Description = g.Key, Index = i })
            .ToList();

        items.AddRange(groupedEncounters.Select(g => new ComboItem(g.Description, g.Index)));

        encounterCombo.DisplayMember = "Text";
        encounterCombo.ValueMember = "Value";
        encounterCombo.DataSource = items;
        encounterCombo.SelectedIndex = 0;
    }

    /// <summary>
    /// Updates the status display with available encounter sources.
    /// </summary>
    private void UpdateSourceDisplay()
    {
        var sources = new List<string>();
        if (_availableSources.HasFlag(EncounterSource.Normal))
            sources.Add("Normal Dens");
        if (_availableSources.HasFlag(EncounterSource.Crystal))
            sources.Add("Crystal");
        if (_availableSources.HasFlag(EncounterSource.Distribution))
            sources.Add("Event");
        if (_availableSources.HasFlag(EncounterSource.Underground))
            sources.Add("Max Lair");

        statusLabel.Text = sources.Count > 0
            ? $"Available in: {string.Join(", ", sources)}"
            : "No encounters found";
    }

    /// <summary>
    /// Gets normal raid encounters for a specific species and game version.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="version">Game version</param>
    /// <returns>List of normal raid encounters</returns>
    private static List<EncounterStatic8N> GetNormalRaidEncounters(ushort species, GameVersion version)
    {
        var encounters = new List<EncounterStatic8N>();
        var source = version == GameVersion.SW ? Encounters8Nest.Nest_SW : Encounters8Nest.Nest_SH;

        foreach (var enc in source)
        {
            if (enc.Species == species)
                encounters.Add(enc);
        }

        return encounters;
    }

    /// <summary>
    /// Gets crystal encounters for a specific species.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <returns>List of crystal encounters</returns>
    private static List<EncounterStatic8NC> GetCrystalEncounters(ushort species)
    {
        var encounters = new List<EncounterStatic8NC>();

        foreach (var enc in Encounters8Nest.Crystal_SWSH)
        {
            if (enc.Species == species)
                encounters.Add(enc);
        }

        return encounters;
    }

    /// <summary>
    /// Gets distribution encounters for a specific species and game version.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="version">Game version</param>
    /// <returns>List of distribution encounters</returns>
    private static List<EncounterStatic8ND> GetDistributionEncounters(ushort species, GameVersion version)
    {
        var encounters = new List<EncounterStatic8ND>();
        var source = version == GameVersion.SW ? Encounters8Nest.Dist_SW : Encounters8Nest.Dist_SH;

        foreach (var enc in source)
        {
            if (enc.Species == species)
                encounters.Add(enc);
        }

        return encounters;
    }

    /// <summary>
    /// Gets underground (Max Lair) encounters for a specific species.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <returns>List of underground encounters</returns>
    private static List<EncounterStatic8U> GetUndergroundEncounters(ushort species)
    {
        var encounters = new List<EncounterStatic8U>();

        foreach (var enc in Encounters8Nest.DynAdv_SWSH)
        {
            if (enc.Species == species)
                encounters.Add(enc);
        }

        return encounters;
    }

    /// <summary>
    /// Handles the search button click event to start or stop seed searching.
    /// </summary>
    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        if (_searchCts != null)
        {
            _searchCts.Cancel();
            return;
        }

        if (speciesCombo.SelectedValue is not int species)
        {
            WinFormsUtil.Alert("Please select a species!");
            return;
        }

        var form = (byte)(formCombo.SelectedValue as int? ?? 0);
        var criteria = GetCriteria();

        // Capture UI values before background task
        var encounterIndex = encounterCombo.SelectedValue as int? ?? -1;
        var selectedEncounterText = (encounterCombo.SelectedItem as ComboItem)?.Text;
        var ivRanges = GetIVRanges();
        var maxResults = (int)maxSeedsNum.Value;

        lock (_resultsLock)
        {
            _results.Clear();
        }
        resultsGrid.Rows.Clear();

        // Hide preview panel when starting new search
        if (_previewPanel != null && _previewPanel.Visible)
        {
            _previewPanel.Visible = false;
            resultsGrid.Height = resultsPanel.Height;
        }

        searchButton.Text = "Stop";
        progressBar.Visible = true;
        statusLabel.Text = "Searching...";

        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => SearchSeeds(species, form, criteria, encounterIndex, selectedEncounterText, ivRanges, maxResults, _searchCts.Token));
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Search cancelled";
        }
        finally
        {
            searchButton.Text = "Search";
            progressBar.Visible = false;
            _searchCts?.Dispose();
            _searchCts = null;
        }
    }

    /// <summary>
    /// Represents an IV range with minimum and maximum values.
    /// </summary>
    private record struct IVRange(int Min, int Max);

    /// <summary>
    /// Gets the encounter criteria from the UI controls.
    /// </summary>
    /// <returns>Encounter criteria for searching</returns>
    private EncounterCriteria GetCriteria()
    {
        var genderIndex = genderCombo.SelectedIndex;
        var gender = genderIndex switch
        {
            0 => Gender.Random,
            1 => (Gender)0, // Male
            2 => (Gender)1, // Female
            3 => (Gender)2, // Genderless
            _ => Gender.Random
        };

        var criteria = new EncounterCriteria
        {
            Gender = gender,
            Ability = GetAbilityPermission(),
            Nature = natureCombo.SelectedIndex == 0 ? Nature.Random : (Nature)(natureCombo.SelectedIndex - 1),
            Shiny = GetShinyType(),
        };

        return criteria;
    }

    /// <summary>
    /// Gets the selected shiny type from the UI.
    /// </summary>
    /// <returns>Shiny type selection</returns>
    private Shiny GetShinyType()
    {
        return shinyCombo.SelectedIndex switch
        {
            0 => Shiny.Random,
            1 => Shiny.Never,
            2 => Shiny.Always,
            3 => Shiny.AlwaysSquare,
            4 => Shiny.AlwaysStar,
            _ => Shiny.Random
        };
    }

    /// <summary>
    /// Gets the IV ranges from the UI controls.
    /// </summary>
    /// <returns>Array of IV ranges for each stat</returns>
    private IVRange[] GetIVRanges()
    {
        return
        [
            new IVRange((int)ivHpMin.Value, (int)ivHpMax.Value),
            new IVRange((int)ivAtkMin.Value, (int)ivAtkMax.Value),
            new IVRange((int)ivDefMin.Value, (int)ivDefMax.Value),
            new IVRange((int)ivSpaMin.Value, (int)ivSpaMax.Value),
            new IVRange((int)ivSpdMin.Value, (int)ivSpdMax.Value),
            new IVRange((int)ivSpeMin.Value, (int)ivSpeMax.Value),
        ];
    }

    /// <summary>
    /// Gets the selected ability permission from the UI.
    /// </summary>
    /// <returns>Ability permission selection</returns>
    private AbilityPermission GetAbilityPermission()
    {
        return abilityCombo.SelectedIndex switch
        {
            0 => AbilityPermission.Any12H,
            1 => AbilityPermission.OnlyFirst,
            2 => AbilityPermission.OnlySecond,
            3 => AbilityPermission.OnlyHidden,
            4 => AbilityPermission.Any12,
            _ => AbilityPermission.Any12H
        };
    }

    /// <summary>
    /// Gets trainer information from the save file.
    /// </summary>
    /// <returns>Trainer info for generation</returns>
    private ITrainerInfo GetTrainerInfo()
    {
        var version = _saveFileEditor.SAV.Version;
        if (version is not (GameVersion.SW or GameVersion.SH))
            version = GameVersion.SW;

        return new SimpleTrainerInfo(version)
        {
            TID16 = (ushort)tidNum.Value,
            SID16 = (ushort)sidNum.Value,
            OT = _saveFileEditor.SAV.OT,
            Gender = _saveFileEditor.SAV.Gender,
            Language = _saveFileEditor.SAV.Language,
        };
    }

    /// <summary>
    /// Searches for seeds that match the specified criteria.
    /// </summary>
    /// <param name="species">Target species ID</param>
    /// <param name="form">Target form</param>
    /// <param name="criteria">Search criteria</param>
    /// <param name="encounterIndex">Selected encounter index</param>
    /// <param name="selectedEncounterText">Selected encounter description</param>
    /// <param name="ivRanges">IV ranges to search for</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <param name="token">Cancellation token</param>
    private void SearchSeeds(int species, byte form, EncounterCriteria criteria, int encounterIndex, string? selectedEncounterText, IVRange[] ivRanges, int maxResults, CancellationToken token)
    {
        var results = new List<SeedResult>();

        // Parse seed range
        if (!TryParseSeedRange(out var startSeed, out var endSeed))
            return;

        ulong totalSeeds = endSeed - startSeed + 1;
        ulong seedsChecked = 0;
        ulong lastProgressUpdate = 0;
        const ulong updateInterval = 50000; // Update less frequently

        var tr = GetTrainerInfo();
        var encountersToCheck = GetEncountersToCheck(form, encounterIndex, selectedEncounterText);

        // Use parallel processing for better performance
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        var seedBatch = new List<ulong>();
        const int batchSize = 10000;

        for (ulong seed = startSeed; seed <= endSeed && results.Count < maxResults && !token.IsCancellationRequested; seed++)
        {
            seedBatch.Add(seed);

            if (seedBatch.Count >= batchSize || seed == endSeed)
            {
                var batchResults = new List<SeedResult>();

                Parallel.ForEach(seedBatch, parallelOptions, currentSeed =>
                {
                    foreach (var wrapper in encountersToCheck)
                    {
                        // First, quickly verify if seed is valid without full generation
                        if (!QuickVerifySeed(wrapper.Encounter, currentSeed, criteria, ivRanges, tr))
                            continue;

                        // Only generate full Pokemon for valid seeds
                        var pk = TryGenerateRaidPokemon(wrapper.Encounter, currentSeed, criteria, tr, form);
                        if (pk == null)
                            continue;

                        // Double-check IVs match what we validated (in case of RNG differences)
                        if (!CheckIVRanges(pk, ivRanges))
                            continue;

                        var result = new SeedResult
                        {
                            Seed = currentSeed,
                            Encounter = wrapper.Encounter,
                            Pokemon = pk
                        };

                        lock (batchResults)
                        {
                            batchResults.Add(result);
                        }
                        break;
                    }
                });

                // Add batch results
                foreach (var result in batchResults.OrderBy(r => r.Seed))
                {
                    if (results.Count >= maxResults)
                        break;

                    results.Add(result);
                    AddResultToGrid(result);
                }

                seedsChecked += (ulong)seedBatch.Count;
                seedBatch.Clear();

                if (seedsChecked - lastProgressUpdate >= updateInterval)
                {
                    lastProgressUpdate = seedsChecked;
                    this.Invoke(() =>
                    {
                        var progressPercent = (int)((seedsChecked / (double)totalSeeds) * 100);
                        progressBar.Value = Math.Min(progressPercent, 100);
                        statusLabel.Text = $"Checked {seedsChecked:N0} ({progressPercent}%), found {results.Count}";
                    });
                }
            }
        }

        lock (_resultsLock)
        {
            _results = results;
        }

        this.Invoke(() =>
        {
            statusLabel.Text = $"Found {results.Count} matches after checking {seedsChecked:N0} seeds";
            progressBar.Value = 100;
        });
    }

    /// <summary>
    /// Quickly verifies if a seed matches the search criteria without generating a full Pokémon.
    /// </summary>
    /// <param name="encounter">Encounter to verify against</param>
    /// <param name="seed">Seed value to check</param>
    /// <param name="criteria">Search criteria</param>
    /// <param name="ivRanges">IV ranges to validate</param>
    /// <param name="tr">Trainer information</param>
    /// <returns>True if the seed potentially matches criteria, false otherwise</returns>
    private bool QuickVerifySeed(object encounter, ulong seed, EncounterCriteria criteria, IVRange[] ivRanges, ITrainerInfo tr)
    {
        var flawlessIVs = encounter switch
        {
            EncounterStatic8N n => n.FlawlessIVCount,
            EncounterStatic8NC nc => nc.FlawlessIVCount,
            EncounterStatic8ND nd => nd.FlawlessIVCount,
            EncounterStatic8U u => u.FlawlessIVCount,
            _ => 0
        };

        var param = encounter switch
        {
            EncounterStatic8N n => new GenerateParam8(n.Species, GetGenderRatio(n), (byte)flawlessIVs, n.Ability, n.Shiny, Nature.Random, n.IVs),
            EncounterStatic8NC nc => new GenerateParam8(nc.Species, GetGenderRatio(nc), (byte)flawlessIVs, nc.Ability, nc.Shiny, Nature.Random, nc.IVs),
            EncounterStatic8ND nd => new GenerateParam8(nd.Species, GetGenderRatio(nd), (byte)flawlessIVs, nd.Ability, nd.Shiny, Nature.Random, nd.IVs),
            EncounterStatic8U u => new GenerateParam8(u.Species, GetGenderRatio(u), (byte)flawlessIVs, u.Ability, Shiny.Never, Nature.Random, u.IVs),
            _ => default
        };

        if (param.Species == 0)
            return false;

        // Quick verification using RNG calculation without full Pokemon generation
        var rng = new Xoroshiro128Plus(seed);
        var ec = (uint)rng.NextInt();

        uint pid;
        bool isShiny;
        {
            var trID = (uint)rng.NextInt();
            pid = (uint)rng.NextInt();
            var xor = PKHeX.Core.ShinyUtil.GetShinyXor(pid, trID);
            isShiny = xor < 16;
        }

        // Check shiny criteria first (fastest check)
        bool matchesShiny = criteria.Shiny switch
        {
            Shiny.Never => !isShiny,
            Shiny.Always => isShiny,
            Shiny.AlwaysSquare => isShiny && (pid ^ tr.ID32) >> 16 == 0,
            Shiny.AlwaysStar => isShiny && (pid ^ tr.ID32) >> 16 > 0 && (pid ^ tr.ID32) >> 16 < 16,
            _ => true
        };

        if (!matchesShiny)
            return false;

        // Quick IV check
        Span<int> ivs = stackalloc int[6];
        ivs.Fill(-1);

        for (int i = 0; i < flawlessIVs; i++)
        {
            int index = (int)rng.NextInt(6);
            while (ivs[index] != -1)
                index = (int)rng.NextInt(6);
            ivs[index] = 31;
        }

        for (int i = 0; i < 6; i++)
        {
            if (ivs[i] == -1)
                ivs[i] = (int)rng.NextInt(32);
        }

        // Check IV ranges
        if (!CheckIVRangesSpan(ivs, ivRanges))
            return false;

        // Check ability if specified
        if (criteria.Ability != AbilityPermission.Any12H)
        {
            int ability = param.Ability switch
            {
                AbilityPermission.Any12H => (int)rng.NextInt(3),
                AbilityPermission.Any12 => (int)rng.NextInt(2),
                _ => param.Ability.GetSingleValue(),
            };

            if (!CheckAbilityQuick(ability, criteria.Ability))
                return false;
        }

        // Check gender if specified
        if (criteria.Gender != Gender.Random)
        {
            byte gender = param.GenderRatio switch
            {
                PersonalInfo.RatioMagicGenderless => 2,
                PersonalInfo.RatioMagicFemale => 1,
                PersonalInfo.RatioMagicMale => 0,
                _ => rng.NextInt(253) + 1 < param.GenderRatio ? (byte)1 : (byte)0,
            };

            if (gender != (byte)criteria.Gender)
                return false;
        }

        // Check nature if specified
        if (criteria.Nature != Nature.Random)
        {
            var nature = param.Nature != Nature.Random ? param.Nature : (Nature)rng.NextInt(25);
            if (nature != criteria.Nature)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the given IVs are within the specified ranges using a Span for performance.
    /// </summary>
    /// <param name="ivs">Span containing the 6 IV values</param>
    /// <param name="ranges">Array of IV ranges to validate against</param>
    /// <returns>True if all IVs are within their respective ranges, false otherwise</returns>
    private static bool CheckIVRangesSpan(Span<int> ivs, IVRange[] ranges)
    {
        return ivs[0] >= ranges[0].Min && ivs[0] <= ranges[0].Max &&
               ivs[1] >= ranges[1].Min && ivs[1] <= ranges[1].Max &&
               ivs[2] >= ranges[2].Min && ivs[2] <= ranges[2].Max &&
               ivs[3] >= ranges[3].Min && ivs[3] <= ranges[3].Max &&
               ivs[4] >= ranges[4].Min && ivs[4] <= ranges[4].Max &&
               ivs[5] >= ranges[5].Min && ivs[5] <= ranges[5].Max;
    }

    /// <summary>
    /// Quickly checks if an ability number matches the specified criteria.
    /// </summary>
    /// <param name="abilityNumber">The ability slot number (0-2)</param>
    /// <param name="criteria">The ability permission criteria</param>
    /// <returns>True if the ability matches criteria, false otherwise</returns>
    private static bool CheckAbilityQuick(int abilityNumber, AbilityPermission criteria)
    {
        return (criteria, abilityNumber) switch
        {
            (AbilityPermission.OnlyFirst, 0) => true,
            (AbilityPermission.OnlySecond, 1) => true,
            (AbilityPermission.OnlyHidden, 2) => true,
            (AbilityPermission.Any12, 0 or 1) => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the gender ratio for an encounter, handling fixed gender encounters.
    /// </summary>
    /// <typeparam name="T">Encounter type that implements IFixedGender</typeparam>
    /// <param name="encounter">The encounter to get gender ratio for</param>
    /// <returns>Gender ratio byte value</returns>
    private static byte GetGenderRatio<T>(T encounter) where T : IFixedGender
    {
        if (encounter.Gender != FixedGenderUtil.GenderRandom)
        {
            return encounter.Gender switch
            {
                0 => PersonalInfo.RatioMagicMale,
                1 => PersonalInfo.RatioMagicFemale,
                2 => PersonalInfo.RatioMagicGenderless,
                _ => 127
            };
        }

        var species = encounter switch
        {
            EncounterStatic8N n => n.Species,
            EncounterStatic8NC nc => nc.Species,
            EncounterStatic8ND nd => nd.Species,
            EncounterStatic8U u => u.Species,
            _ => (ushort)0
        };

        var form = encounter switch
        {
            EncounterStatic8N n => n.Form,
            EncounterStatic8NC nc => nc.Form,
            EncounterStatic8ND nd => nd.Form,
            EncounterStatic8U u => u.Form,
            _ => (byte)0
        };

        return PersonalTable.SWSH[species, form].Gender;
    }

    /// <summary>
    /// Tries to parse the seed range from the UI text boxes.
    /// </summary>
    /// <param name="startSeed">Parsed start seed value</param>
    /// <param name="endSeed">Parsed end seed value</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    private bool TryParseSeedRange(out ulong startSeed, out ulong endSeed)
    {
        startSeed = 0x0000000000000000;
        endSeed = 0xFFFFFFFFFFFFFFFF;

        if (!string.IsNullOrEmpty(startSeedTextBox?.Text))
        {
            if (!ulong.TryParse(startSeedTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out startSeed))
            {
                this.Invoke(() => WinFormsUtil.Alert("Invalid start seed format. Using default 0000000000000000."));
                return false;
            }
        }

        if (!string.IsNullOrEmpty(endSeedTextBox?.Text))
        {
            if (!ulong.TryParse(endSeedTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out endSeed))
            {
                this.Invoke(() => WinFormsUtil.Alert("Invalid end seed format. Using default FFFFFFFFFFFFFFFF."));
                return false;
            }
        }

        if (startSeed > endSeed)
        {
            this.Invoke(() => WinFormsUtil.Alert("Start seed must be less than or equal to end seed!"));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the list of encounters to check based on form and selection.
    /// </summary>
    /// <param name="form">Target form</param>
    /// <param name="encounterIndex">Selected encounter index</param>
    /// <param name="selectedEncounterText">Selected encounter description</param>
    /// <returns>List of encounter wrappers to check</returns>
    private List<EncounterWrapper> GetEncountersToCheck(byte form, int encounterIndex, string? selectedEncounterText)
    {
        if (encounterIndex == -1)
        {
            return _cachedEncounters.Where(e => e.Form == form || e.Form >= EncounterUtil.FormDynamic).ToList();
        }

        return _cachedEncounters.Where(e => e.GetDescription() == selectedEncounterText && (e.Form == form || e.Form >= EncounterUtil.FormDynamic)).ToList();
    }

    /// <summary>
    /// Tries to generate a raid Pokémon from an encounter and seed.
    /// </summary>
    /// <param name="encounter">Encounter to generate from</param>
    /// <param name="seed">Seed value</param>
    /// <param name="criteria">Generation criteria</param>
    /// <param name="tr">Trainer information</param>
    /// <param name="desiredForm">Desired form</param>
    /// <returns>Generated PK8 if successful, null otherwise</returns>
    private PK8? TryGenerateRaidPokemon(object encounter, ulong seed, EncounterCriteria criteria, ITrainerInfo tr, byte desiredForm)
    {
        try
        {
            PK8? pk8 = null;

            switch (encounter)
            {
                case EncounterStatic8N n:
                    pk8 = n.ConvertToPKM(tr, criteria);
                    n.GenerateSeed64(pk8, seed);
                    break;
                case EncounterStatic8NC nc:
                    pk8 = nc.ConvertToPKM(tr, criteria);
                    nc.GenerateSeed64(pk8, seed);
                    break;
                case EncounterStatic8ND nd:
                    pk8 = nd.ConvertToPKM(tr, criteria);
                    nd.GenerateSeed64(pk8, seed);
                    break;
                case EncounterStatic8U u:
                    pk8 = u.ConvertToPKM(tr, criteria);
                    u.GenerateSeed64(pk8, seed);
                    break;
            }

            if (pk8 == null)
                return null;

            var baseForm = encounter switch
            {
                EncounterStatic8N n => n.Form,
                EncounterStatic8NC nc => nc.Form,
                EncounterStatic8ND nd => nd.Form,
                EncounterStatic8U u => u.Form,
                _ => (byte)0
            };

            if (baseForm < EncounterUtil.FormDynamic && pk8.Form != desiredForm)
            {
                return null;
            }

            // Ensure stats are calculated
            pk8.ResetPartyStats();

            return pk8;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a generated Pokémon matches the search criteria.
    /// </summary>
    /// <param name="pk">Generated Pokémon</param>
    /// <param name="criteria">Search criteria</param>
    /// <param name="ivRanges">IV ranges to check</param>
    /// <returns>True if matches criteria, false otherwise</returns>
    private bool CheckPokemonMatchesCriteria(PK8 pk, EncounterCriteria criteria, IVRange[] ivRanges)
    {
        // Check shiny
        bool matchesShiny = criteria.Shiny switch
        {
            Shiny.Never => !pk.IsShiny,
            Shiny.Always => pk.IsShiny,
            Shiny.AlwaysSquare => pk.IsShiny && pk.ShinyXor == 0,
            Shiny.AlwaysStar => pk.IsShiny && pk.ShinyXor > 0 && pk.ShinyXor < 16,
            _ => true
        };

        if (!matchesShiny)
            return false;

        // Check gender
        if (criteria.Gender != Gender.Random && pk.Gender != (int)criteria.Gender)
            return false;

        // Check nature
        if (criteria.Nature != Nature.Random && pk.Nature != criteria.Nature)
            return false;

        // Check IVs
        if (!CheckIVRanges(pk, ivRanges))
            return false;

        // Check ability
        if (criteria.Ability != AbilityPermission.Any12H && !CheckAbilityCriteria(pk, criteria.Ability))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a Pokémon's ability matches the specified criteria.
    /// </summary>
    /// <param name="pk">Pokémon to check</param>
    /// <param name="criteria">Ability criteria</param>
    /// <returns>True if ability matches, false otherwise</returns>
    private static bool CheckAbilityCriteria(PK8 pk, AbilityPermission criteria)
    {
        var pi = PersonalTable.SWSH[pk.Species, pk.Form];

        return (criteria, pk.AbilityNumber) switch
        {
            (AbilityPermission.OnlyFirst, 1) => pk.Ability == pi.Ability1,
            (AbilityPermission.OnlySecond, 2) => pk.Ability == pi.Ability2,
            (AbilityPermission.OnlyHidden, 4) => pk.Ability == pi.AbilityH,
            (AbilityPermission.Any12, 1) => pk.Ability == pi.Ability1,
            (AbilityPermission.Any12, 2) => pk.Ability == pi.Ability2,
            (_, _) when criteria == AbilityPermission.Any12H => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a Pokémon's IVs are within the specified ranges.
    /// </summary>
    /// <param name="pk">Pokémon to check</param>
    /// <param name="ranges">IV ranges to validate against</param>
    /// <returns>True if all IVs are within range, false otherwise</returns>
    private static bool CheckIVRanges(PK8 pk, IVRange[] ranges)
    {
        return pk.IV_HP >= ranges[0].Min && pk.IV_HP <= ranges[0].Max &&
               pk.IV_ATK >= ranges[1].Min && pk.IV_ATK <= ranges[1].Max &&
               pk.IV_DEF >= ranges[2].Min && pk.IV_DEF <= ranges[2].Max &&
               pk.IV_SPA >= ranges[3].Min && pk.IV_SPA <= ranges[3].Max &&
               pk.IV_SPD >= ranges[4].Min && pk.IV_SPD <= ranges[4].Max &&
               pk.IV_SPE >= ranges[5].Min && pk.IV_SPE <= ranges[5].Max;
    }

    /// <summary>
    /// Adds a seed result to the results grid.
    /// </summary>
    /// <param name="result">Seed result to add</param>
    private void AddResultToGrid(SeedResult result)
    {
        this.Invoke(() =>
        {
            var wrapper = new EncounterWrapper(result.Encounter, GameVersion.SWSH);
            var stars = GetStarRating(result.Encounter);
            var shinyType = result.Pokemon.IsShiny ? (result.Pokemon.ShinyXor == 0 ? "■" : "★") : "";

            var row = resultsGrid.Rows.Add(
                $"{result.Seed:X16}",
                wrapper.GetShortDescription(),
                stars,
                shinyType,
                result.Pokemon.Nature.ToString(),
                GetAbilityName(result.Pokemon),
                GetIVString(result.Pokemon),
                result.Pokemon.CanGigantamax ? "G-Max" : "-"
            );

            // Store the result index in the row tag for easy retrieval
            resultsGrid.Rows[row].Tag = result;

            if (result.Pokemon.IsShiny)
            {
                var isSquare = result.Pokemon.ShinyXor == 0;
                resultsGrid.Rows[row].DefaultCellStyle.BackColor = isSquare ? Color.FromArgb(32, 32, 64) : Color.FromArgb(64, 64, 32);
                resultsGrid.Rows[row].DefaultCellStyle.ForeColor = isSquare ? Color.DeepSkyBlue : Color.Gold;
                resultsGrid.Rows[row].DefaultCellStyle.SelectionBackColor = isSquare ? Color.DarkBlue : Color.DarkGoldenrod;
                resultsGrid.Rows[row].DefaultCellStyle.SelectionForeColor = Color.White;
            }
        });
    }

    /// <summary>
    /// Gets the star rating (flawless IV count) for an encounter.
    /// </summary>
    /// <param name="encounter">Encounter to check</param>
    /// <returns>Star rating string</returns>
    private static string GetStarRating(object encounter)
    {
        return encounter switch
        {
            EncounterStatic8N n => $"{n.FlawlessIVCount}IV",
            EncounterStatic8ND nd => $"{nd.FlawlessIVCount}IV",
            EncounterStatic8NC nc => $"{nc.FlawlessIVCount}IV",
            EncounterStatic8U u => $"{u.FlawlessIVCount}IV",
            _ => "?"
        };
    }

    /// <summary>
    /// Gets the ability name for a Pokémon.
    /// </summary>
    /// <param name="pk">Pokémon to check</param>
    /// <returns>Ability name string</returns>
    private static string GetAbilityName(PK8 pk)
    {
        var abilities = PersonalTable.SWSH[pk.Species, pk.Form];
        return pk.AbilityNumber switch
        {
            1 => GameInfo.Strings.abilitylist[abilities.Ability1],
            2 => GameInfo.Strings.abilitylist[abilities.Ability2],
            4 => GameInfo.Strings.abilitylist[abilities.AbilityH],
            _ => "?"
        };
    }

    /// <summary>
    /// Gets a formatted IV string for a Pokémon.
    /// </summary>
    /// <param name="pk">Pokémon to check</param>
    /// <returns>Formatted IV string</returns>
    private static string GetIVString(PK8 pk)
    {
        return $"{pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
    }

    /// <summary>
    /// Handles double-click events on the results grid to load Pokémon into the editor.
    /// </summary>
    private void ResultsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= resultsGrid.Rows.Count)
            return;

        var result = resultsGrid.Rows[e.RowIndex].Tag as SeedResult;
        if (result == null)
            return;

        try
        {
            _pkmEditor.PopulateFields(result.Pokemon);

            var wrapper = new EncounterWrapper(result.Encounter, GameVersion.SWSH);
            WinFormsUtil.Alert($"Loaded {result.Pokemon.Nickname}!\nSeed: {result.Seed:X16}\nEncounter: {wrapper.GetDescription()}");
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Failed to load Pokémon: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the export button click to save results to CSV.
    /// </summary>
    private void ExportButton_Click(object? sender, EventArgs e)
    {
        if (_results.Count == 0)
        {
            WinFormsUtil.Alert("No results to export!");
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"Gen8_Seeds_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var writer = new System.IO.StreamWriter(sfd.FileName);
            writer.WriteLine($"Seed,Encounter,Stars,Shiny,Nature,Ability,IVs,Gigantamax,EC,PID,TID,SID");

            foreach (var result in _results)
            {
                var wrapper = new EncounterWrapper(result.Encounter, GameVersion.SWSH);
                var stars = GetStarRating(result.Encounter);
                var shinyType = result.Pokemon.IsShiny ? (result.Pokemon.ShinyXor == 0 ? "Square" : "Star") : "No";

                writer.WriteLine($"{result.Seed:X16},{wrapper.GetDescription()},{stars},{shinyType}," +
                               $"{result.Pokemon.Nature},{GetAbilityName(result.Pokemon)},{GetIVString(result.Pokemon)}," +
                               $"{(result.Pokemon.CanGigantamax ? "Yes" : "No")},{result.Pokemon.EncryptionConstant:X8}," +
                               $"{result.Pokemon.PID:X8},{result.Pokemon.TID16},{result.Pokemon.SID16}");
            }

            WinFormsUtil.Alert("Export successful!");
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles source checkbox changes to update encounter lists.
    /// </summary>
    private void SourceCheckChanged(object? sender, EventArgs e)
    {
        if (speciesCombo.SelectedValue is int species)
        {
            UpdateEncounterList(species);
            UpdateSourceDisplay();
            UpdateEncounterCombo();
        }
    }

    /// <summary>
    /// Represents a seed search result.
    /// </summary>
    private class SeedResult
    {
        /// <summary>
        /// The seed value that generated this result.
        /// </summary>
        public ulong Seed { get; set; }

        /// <summary>
        /// The encounter that was used for generation.
        /// </summary>
        public object Encounter { get; set; } = null!;

        /// <summary>
        /// The generated Pokémon.
        /// </summary>
        public PK8 Pokemon { get; set; } = null!;
    }

    /// <summary>
    /// Combo box item for display.
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="value">Associated value</param>
    private class ComboItem(string text, int value)
    {
        /// <summary>
        /// Display text for the item.
        /// </summary>
        public string Text { get; } = text;

        /// <summary>
        /// Associated value for the item.
        /// </summary>
        public int Value { get; } = value;
    }

    /// <summary>
    /// Wrapper for encounter objects with version information.
    /// </summary>
    private class EncounterWrapper
    {
        /// <summary>
        /// The wrapped encounter object.
        /// </summary>
        public object Encounter { get; }

        /// <summary>
        /// The game version for this encounter.
        /// </summary>
        public GameVersion Version { get; }

        /// <summary>
        /// Initializes a new instance of the EncounterWrapper class.
        /// </summary>
        /// <param name="encounter">Encounter to wrap</param>
        /// <param name="version">Game version</param>
        public EncounterWrapper(object encounter, GameVersion version)
        {
            Encounter = encounter;
            Version = version;
        }

        /// <summary>
        /// Gets the form of the wrapped encounter.
        /// </summary>
        public byte Form => Encounter switch
        {
            EncounterStatic8N n => n.Form,
            EncounterStatic8NC nc => nc.Form,
            EncounterStatic8ND nd => nd.Form,
            EncounterStatic8U u => u.Form,
            _ => 0
        };

        /// <summary>
        /// Gets a full description of the encounter.
        /// </summary>
        /// <returns>Description string</returns>
        public string GetDescription()
        {
            return Encounter switch
            {
                EncounterStatic8N n => $"{GetVersionString()} Normal Den - {(n.LevelMax - 15) / 10}★",
                EncounterStatic8NC => "Crystal Den",
                EncounterStatic8ND nd => $"{GetVersionString()} Event #{nd.Index}",
                EncounterStatic8U => "Max Lair",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets a short description of the encounter.
        /// </summary>
        /// <returns>Short description string</returns>
        public string GetShortDescription()
        {
            return Encounter switch
            {
                EncounterStatic8N n => $"{GetVersionString()} Normal",
                EncounterStatic8NC => "Crystal",
                EncounterStatic8ND => $"{GetVersionString()} Event",
                EncounterStatic8U => "Max Lair",
                _ => "?"
            };
        }

        /// <summary>
        /// Gets the version string for display.
        /// </summary>
        /// <returns>Version string</returns>
        private string GetVersionString()
        {
            return Version switch
            {
                GameVersion.SW => "SW",
                GameVersion.SH => "SH",
                _ => ""
            };
        }
    }
}
