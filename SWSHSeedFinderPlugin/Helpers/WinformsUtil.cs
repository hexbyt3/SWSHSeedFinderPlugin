using System;
using System.Media;
using System.Windows.Forms;

namespace SWSHSeedFinderPlugin.Helpers;

/// <summary>
/// Minimal WinForms utility methods for displaying messages
/// </summary>
public static class WinFormsUtil
{
    /// <summary>
    /// Displays an information dialog with the provided message lines.
    /// </summary>
    /// <param name="lines">Lines to display in the message box.</param>
    /// <returns>The <see cref="DialogResult"/> associated with the dialog.</returns>
    public static DialogResult Alert(params string[] lines)
    {
        SystemSounds.Asterisk.Play();
        string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
        return MessageBox.Show(msg, "SV Seed Finder", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Displays a dialog showing the details of an error.
    /// </summary>
    /// <param name="lines">User-friendly message about the error.</param>
    /// <returns>The <see cref="DialogResult"/> associated with the dialog.</returns>
    public static DialogResult Error(params string[] lines)
    {
        SystemSounds.Hand.Play();
        string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
        return MessageBox.Show(msg, "SV Seed Finder - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
