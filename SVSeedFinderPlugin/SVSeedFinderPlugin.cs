using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SVSeedFinderPlugin.Helpers;
using PKHeX.Core;

namespace SVSeedFinderPlugin;

/// <summary>
/// PKHeX Plugin for finding Generation 9 Tera Raid seeds that match specific criteria
/// </summary>
public sealed class SVSeedFinderPlugin : IPlugin
{
    public string Name => "SV Seed Finder";
    public int Priority => 1;

    // Initialized on plugin load
    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PKMEditor { get; private set; } = null!;

    public void Initialize(params object[] args)
    {
        Console.WriteLine($"Loading {Name}...");

        // Check version compatibility
        if (PluginVersion.HasVersionMismatch())
        {
            Console.WriteLine($"[{Name}] {PluginVersion.GetCompatibilityMessage()}");
        }

        SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
        PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
        var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip)!;
        LoadMenuStrip(menu);
    }

    private void LoadMenuStrip(ToolStrip menuStrip)
    {
        var items = menuStrip.Items;
        if (items.Find("Menu_Tools", false)[0] is not ToolStripDropDownItem tools)
            throw new ArgumentException(null, nameof(menuStrip));
        AddPluginControl(tools);
    }

    private void AddPluginControl(ToolStripDropDownItem tools)
    {
        var ctrl = new ToolStripMenuItem(Name)
        {
            ShortcutKeys = Keys.Control | Keys.F,
            Image = CreateTeraCrystalIcon()
        };

        ctrl.Click += (_, _) => ShowSeedFinderForm();
        tools.DropDownItems.Add(ctrl);
        Console.WriteLine($"{Name} added menu item.");
    }

    private static Bitmap CreateTeraCrystalIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var points = new Point[]
            {
                new(8, 2),
                new(13, 8),
                new(8, 14),
                new(3, 8)
            };

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 16, 16),
                Color.FromArgb(200, 138, 43, 226),
                Color.FromArgb(200, 75, 0, 130),
                45f))
            {
                g.FillPolygon(brush, points);
            }

            g.DrawPolygon(new Pen(Color.DarkViolet, 1), points);
        }
        return bmp;
    }

    private void ShowSeedFinderForm()
    {
        try
        {
            using var form = new GUI.Gen9SeedFinderForm(SaveFileEditor, PKMEditor);
            form.ShowDialog();
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Error loading {Name}:", ex.Message);
        }
    }

    public void NotifySaveLoaded()
    {
        Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
    }

    public bool TryLoadFile(string filePath)
    {
        Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
        return false; // no action taken
    }
}
