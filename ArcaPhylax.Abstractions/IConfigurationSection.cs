namespace ArcaPhylax.Abstractions;

/// <summary>
/// A configuration section contributed to the Configure page by the host or a plugin. Mirrors
/// <see cref="ITabContentViewModel"/>: implement this on a view-model that has a matching view
/// (resolved by the <c>ViewModel</c>-&gt;<c>View</c> naming convention), register it
/// <c>As&lt;IConfigurationSection&gt;()</c>, and the Configure page will render it as a card.
/// </summary>
public interface IConfigurationSection
{
    /// <summary>Heading shown on the section's card.</summary>
    string Title { get; }

    /// <summary>Ordering weight; sections are sorted ascending, so a lower value sits higher.</summary>
    int Order => 0;
}
