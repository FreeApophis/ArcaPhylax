namespace ArcaPhylax.Abstractions;

public interface ITabContentViewModel
{
    string Title { get; }

    string Icon { get; }

    /// <summary>
    /// Ordering weight for the navigation rail; tabs are sorted ascending, so a lower weight sits
    /// higher up. Defaults to 0. (The host pins its Configure and About pages to the very bottom
    /// regardless of weight.)
    /// </summary>
    int NavigationWeight => 0;
}
