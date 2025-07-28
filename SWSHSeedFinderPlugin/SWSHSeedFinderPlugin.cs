using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PKHeX.Core;
using SWSHSeedFinderPlugin.Helpers;

namespace SWSHSeedFinderPlugin;

/// <summary>
/// PKHeX Plugin for finding Generation 9 Tera Raid seeds that match specific criteria
/// </summary>
public sealed class SWSHSeedFinderPlugin : IPlugin
{
    public string Name => "Gen 8 Seed Finder";
    public int Priority => 1;

    // Initialized on plugin load
    public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
    public IPKMView PKMEditor { get; private set; } = null!;

    public void Initialize(params object[] args)
    {
        Console.WriteLine($"Loading {Name}...");

        // Check version compatibility
        if (PluginVersion.HasVersionMismatch())
            Console.WriteLine($"[{Name}] {PluginVersion.GetCompatibilityMessage()}");

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
            Image = CreateRaidCrystalIcon()
        };

        ctrl.Click += (_, _) => ShowSeedFinderForm();
        tools.DropDownItems.Add(ctrl);
        Console.WriteLine($"{Name} added menu item.");
    }

    private static Bitmap CreateRaidCrystalIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Create a diamond/crystal shape
            var points = new Point[]
            {
                new(8, 2),
                new(13, 8),
                new(8, 14),
                new(3, 8)
            };

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 16, 16),
                Color.FromArgb(200, 30, 144, 255),
                Color.FromArgb(200, 255, 20, 147),
                45f))
            {
                g.FillPolygon(brush, points);
            }

            using (var pen = new Pen(Color.FromArgb(255, 0, 100, 200), 1))
            {
                g.DrawPolygon(pen, points);
            }

            var shinePoints = new Point[]
            {
                new(8, 4),
                new(10, 6),
                new(8, 8),
                new(6, 6)
            };

            using var shineBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
            g.FillPolygon(shineBrush, shinePoints);
        }
        return bmp;
    }

    private void ShowSeedFinderForm()
    {
        try
        {
            using var form = new GUI.Gen8SeedFinderForm(SaveFileEditor, PKMEditor);
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
