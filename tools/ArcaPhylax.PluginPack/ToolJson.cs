using System.Text.Encodings.Web;
using System.Text.Json;

namespace ArcaPhylax.PluginPack;

/// <summary>JSON for console output meant to be copied into config files: indented, camelCase, no escaping of '+' etc.</summary>
internal static class ToolJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
