namespace ArcaPhylax.PluginPackaging.Tests.Fakes;

/// <summary>A throwaway plugin store root directory.</summary>
public sealed class TempStoreRoot : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arca-store-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Not worth failing a test over.
        }
    }
}
