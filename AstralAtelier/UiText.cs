using System.Globalization;
using System.Text.Json;

internal static class UiText
{
    private static readonly Dictionary<string, string> Labels =
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(G2Util.FindFilePath("resource/ui/texts.ko.json")))
        ?? throw new InvalidDataException("UI text resource is empty.");

    public static string Get(string key, params object[] values)
    {
        string label = Labels[key];
        return values.Length == 0 ? label : string.Format(CultureInfo.InvariantCulture, label, values);
    }
}
