#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ShawarmaTycoon.Tests
{
    /// <summary>
    /// The game ships two Latin text faces and nothing else, so a character
    /// neither of them carries draws as nothing in the player.
    ///
    /// The editor cannot warn about this and neither can Unity: for a dynamic
    /// font, Font.HasCharacter answers true for every character ever asked,
    /// because on Windows the editor quietly borrows a glyph from the system.
    /// The web build has nothing to borrow from. That is how the tick, star,
    /// note and grid buttons went out blank, and how every upgrade card showed
    /// an empty level row. The only honest answer is in the font file, so this
    /// reads the character table out of the TTF itself.
    ///
    /// Button icons are drawn as sprites now (see UITheme), which the compiler
    /// enforces. This covers what is left: characters written inline in text.
    /// </summary>
    public sealed class FontCoverageTests
    {
        /// <summary>
        /// Drawn only with DisplayFont — ItemStation's world label — so Nunito
        /// not carrying it costs nothing.
        /// </summary>
        private static readonly HashSet<char> DisplayFontOnly = new() { '→' };

        private static readonly Regex StringLiteral =
            new("\"((?:[^\"\\\\\\n]|\\\\.)*)\"", RegexOptions.Compiled);

        [Test]
        public void ShippedFontsCarryEveryCharacterTheUiDraws()
        {
            HashSet<int> body = ReadCoverage("Nunito-Variable.ttf");
            HashSet<int> display = ReadCoverage("Baloo2-Variable.ttf");

            List<string> missing = new();
            string root = Path.Combine(Application.dataPath, "ShawarmaTycoon", "Scripts");

            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();
                    // A comment never reaches a label, and console output is
                    // read in a devtools window with its own fonts.
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("*")) continue;
                    if (line.Contains("Debug.Log")) continue;

                    foreach (Match match in StringLiteral.Matches(line))
                    {
                        foreach (char c in match.Groups[1].Value)
                        {
                            if (c < 128) continue;
                            bool drawable = display.Contains(c)
                                && (body.Contains(c) || DisplayFontOnly.Contains(c));
                            if (drawable) continue;
                            missing.Add(
                                $"'{c}' (U+{(int)c:X4}) {Path.GetFileName(file)}:{i + 1}");
                        }
                    }
                }
            }

            Assert.IsEmpty(missing,
                "Bu karakterler paketlenen yazı tiplerinde yok, oyunda hiç çizilmezler. "
                + "İkon gerekiyorsa UITheme'e sprite olarak ekleyin:\n  "
                + string.Join("\n  ", missing));
        }

        [Test]
        public void EveryDrawnIconHasInk()
        {
            Sprite[] icons =
            {
                UI.UITheme.Check, UI.UITheme.Cross, UI.UITheme.Star,
                UI.UITheme.Note, UI.UITheme.Grid
            };

            foreach (Sprite icon in icons)
            {
                Assert.IsNotNull(icon, "İkon üretilemedi");
                Color[] pixels = icon.texture.GetPixels();
                int opaque = 0;
                foreach (Color pixel in pixels)
                    if (pixel.a > 0.9f) opaque++;

                // A shape that covers almost nothing or almost everything is a
                // botched drawing, not an icon.
                float ratio = opaque / (float)pixels.Length;
                Assert.Greater(ratio, 0.02f, $"{icon.name} neredeyse boş");
                Assert.Less(ratio, 0.75f, $"{icon.name} neredeyse dolu");
            }
        }

        // ---- TTF character table -------------------------------------------

        private static HashSet<int> ReadCoverage(string fileName)
        {
            string path = Path.Combine(
                Application.dataPath, "ShawarmaTycoon", "Resources", "Fonts", fileName);
            Assert.IsTrue(File.Exists(path), $"{fileName} bulunamadı");

            byte[] font = File.ReadAllBytes(path);
            int tableCount = ReadUInt16(font, 4);
            int cmap = -1;
            for (int i = 0; i < tableCount; i++)
            {
                int record = 12 + i * 16;
                if (Encoding.ASCII.GetString(font, record, 4) == "cmap")
                    cmap = (int)ReadUInt32(font, record + 8);
            }
            Assert.AreNotEqual(-1, cmap, $"{fileName} içinde cmap tablosu yok");

            HashSet<int> covered = new();
            int subtableCount = ReadUInt16(font, cmap + 2);
            for (int i = 0; i < subtableCount; i++)
            {
                int subtable = cmap + (int)ReadUInt32(font, cmap + 4 + i * 8 + 4);
                int format = ReadUInt16(font, subtable);
                if (format == 4) ReadFormat4(font, subtable, covered);
                else if (format == 12) ReadFormat12(font, subtable, covered);
            }
            return covered;
        }

        private static void ReadFormat4(byte[] font, int subtable, HashSet<int> covered)
        {
            int segmentsX2 = ReadUInt16(font, subtable + 6);
            int ends = subtable + 14;
            int starts = ends + segmentsX2 + 2;
            int deltas = starts + segmentsX2;
            int ranges = deltas + segmentsX2;

            for (int s = 0; s < segmentsX2 / 2; s++)
            {
                int end = ReadUInt16(font, ends + s * 2);
                int start = ReadUInt16(font, starts + s * 2);
                short delta = (short)ReadUInt16(font, deltas + s * 2);
                int rangeOffset = ReadUInt16(font, ranges + s * 2);
                if (start == 0xFFFF) continue;

                for (int c = start; c <= end && c != 0x10000; c++)
                {
                    int glyph;
                    if (rangeOffset == 0) glyph = (c + delta) & 0xFFFF;
                    else
                    {
                        int index = ranges + s * 2 + rangeOffset + (c - start) * 2;
                        if (index + 1 >= font.Length) continue;
                        glyph = ReadUInt16(font, index);
                        if (glyph != 0) glyph = (glyph + delta) & 0xFFFF;
                    }
                    if (glyph != 0) covered.Add(c);
                }
            }
        }

        private static void ReadFormat12(byte[] font, int subtable, HashSet<int> covered)
        {
            long groups = ReadUInt32(font, subtable + 12);
            for (long g = 0; g < groups; g++)
            {
                int record = subtable + 16 + (int)g * 12;
                long start = ReadUInt32(font, record);
                long end = ReadUInt32(font, record + 4);
                for (long c = start; c <= end; c++) covered.Add((int)c);
            }
        }

        private static int ReadUInt16(byte[] data, int offset) =>
            (data[offset] << 8) | data[offset + 1];

        private static uint ReadUInt32(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8) | data[offset + 3];
    }
}
#endif
