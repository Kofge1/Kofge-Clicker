using System.Text;

namespace KofgeClicker;

public sealed class IniFile
{
    private static readonly object FileLock = new();
    private static readonly Encoding FileEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly string _path;

    public IniFile(string path)
    {
        _path = path;
    }

    public string ReadString(string section, string key, string defaultValue = "")
    {
        lock (FileLock)
        {
            if (!File.Exists(_path))
            {
                return defaultValue;
            }

            var sectionHeader = $"[{section}]";
            var inSection = false;
            foreach (var line in File.ReadLines(_path))
            {
                if (IsSectionHeader(line))
                {
                    inSection = string.Equals(line.Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inSection)
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0
                    || !string.Equals(line[..separatorIndex].Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return line[(separatorIndex + 1)..].Trim();
            }

            return defaultValue;
        }
    }

    public int ReadInt(string section, string key, int defaultValue = 0)
    {
        var text = ReadString(section, key, defaultValue.ToString());
        return int.TryParse(text, out var value) ? value : defaultValue;
    }

    public bool ReadBool(string section, string key, bool defaultValue = false)
    {
        var text = ReadString(section, key, defaultValue ? "1" : "0");
        return text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public void WriteString(string section, string key, string value)
    {
        UpdateSection(section, [new(key, value)]);
    }

    public void WriteInt(string section, string key, int value)
    {
        WriteString(section, key, value.ToString());
    }

    public void WriteBool(string section, string key, bool value)
    {
        WriteString(section, key, value ? "1" : "0");
    }

    public void WriteSection(string section, IEnumerable<KeyValuePair<string, string>> values)
    {
        lock (FileLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? AppContext.BaseDirectory);

            var lines = File.Exists(_path)
                ? File.ReadAllLines(_path).ToList()
                : [];
            var sectionHeader = $"[{section}]";
            var sectionStart = lines.FindIndex(line => string.Equals(line.Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase));
            var newSectionLines = new List<string> { sectionHeader };
            newSectionLines.AddRange(values.Select(pair => $"{pair.Key}={pair.Value}"));

            if (sectionStart < 0)
            {
                if (lines.Count > 0 && lines[^1].Length > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.AddRange(newSectionLines);
                File.WriteAllLines(_path, lines, FileEncoding);
                return;
            }

            var sectionEnd = sectionStart + 1;
            while (sectionEnd < lines.Count && !IsSectionHeader(lines[sectionEnd]))
            {
                sectionEnd++;
            }

            lines.RemoveRange(sectionStart, sectionEnd - sectionStart);
            lines.InsertRange(sectionStart, newSectionLines);
            File.WriteAllLines(_path, lines, FileEncoding);
        }
    }

    public void UpdateSection(string section, IEnumerable<KeyValuePair<string, string>> values)
    {
        lock (FileLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? AppContext.BaseDirectory);

            var lines = File.Exists(_path)
                ? File.ReadAllLines(_path).ToList()
                : [];
            var updates = values.ToList();
            var sectionHeader = $"[{section}]";
            var matchingSections = FindSectionRanges(lines, sectionHeader);
            if (matchingSections.Count == 0)
            {
                if (lines.Count > 0 && lines[^1].Length > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add(sectionHeader);
                lines.AddRange(updates.Select(pair => $"{pair.Key}={pair.Value}"));
                File.WriteAllLines(_path, lines, FileEncoding);
                return;
            }

            var updatedValues = updates.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            var writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newSectionLines = new List<string> { sectionHeader };
            foreach (var (sectionStart, sectionEnd) in matchingSections)
            {
                for (var i = sectionStart + 1; i < sectionEnd; i++)
                {
                    var line = lines[i];
                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        newSectionLines.Add(line);
                        continue;
                    }

                    var key = line[..separatorIndex].Trim();
                    if (!writtenKeys.Add(key))
                    {
                        continue;
                    }

                    newSectionLines.Add(updatedValues.TryGetValue(key, out var updatedValue)
                        ? $"{key}={updatedValue}"
                        : line);
                }
            }

            foreach (var pair in updates)
            {
                if (writtenKeys.Add(pair.Key))
                {
                    newSectionLines.Add($"{pair.Key}={pair.Value}");
                }
            }

            var insertionIndex = matchingSections[0].Start;
            for (var i = matchingSections.Count - 1; i >= 0; i--)
            {
                var range = matchingSections[i];
                lines.RemoveRange(range.Start, range.End - range.Start);
            }

            lines.InsertRange(insertionIndex, newSectionLines);
            File.WriteAllLines(_path, lines, FileEncoding);
        }
    }

    public void NormalizeSection(string section)
    {
        lock (FileLock)
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var sectionHeader = $"[{section}]";
            if (File.ReadLines(_path).Count(line => string.Equals(line.Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                UpdateSection(section, []);
            }
        }
    }

    public void DeleteKey(string section, string key)
    {
        lock (FileLock)
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var lines = File.ReadAllLines(_path).ToList();
            var ranges = FindSectionRanges(lines, $"[{section}]");
            for (var rangeIndex = ranges.Count - 1; rangeIndex >= 0; rangeIndex--)
            {
                var range = ranges[rangeIndex];
                for (var i = range.End - 1; i > range.Start; i--)
                {
                    var separatorIndex = lines[i].IndexOf('=');
                    if (separatorIndex > 0
                        && string.Equals(lines[i][..separatorIndex].Trim(), key, StringComparison.OrdinalIgnoreCase))
                    {
                        lines.RemoveAt(i);
                    }
                }
            }

            File.WriteAllLines(_path, lines, FileEncoding);
        }
    }

    public void DeleteSection(string section)
    {
        lock (FileLock)
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var lines = File.ReadAllLines(_path).ToList();
            var ranges = FindSectionRanges(lines, $"[{section}]");
            for (var i = ranges.Count - 1; i >= 0; i--)
            {
                var range = ranges[i];
                lines.RemoveRange(range.Start, range.End - range.Start);
            }

            File.WriteAllLines(_path, lines, FileEncoding);
        }
    }

    private static bool IsSectionHeader(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']';
    }

    private static List<(int Start, int End)> FindSectionRanges(List<string> lines, string sectionHeader)
    {
        List<(int Start, int End)> ranges = [];
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.Equals(lines[i].Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var end = i + 1;
            while (end < lines.Count && !IsSectionHeader(lines[end]))
            {
                end++;
            }

            ranges.Add((i, end));
            i = end - 1;
        }

        return ranges;
    }
}
