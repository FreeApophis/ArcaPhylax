using ArcaPhylax.CharacterCards.Cards;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CharacterPlugin.Views;

/// <summary>
/// Presents a character card image as a rounded 3D "pill" — dark border, soft drop shadow, a
/// translucent name ribbon and a fading mirror reflection — falling back to a square placeholder
/// when <see cref="Source"/> is null. Bind <see cref="Source"/>, <see cref="CharacterName"/> and
/// <see cref="Spec"/>; the V1/V2 corner badge derives from <see cref="Spec"/>.
/// </summary>
public sealed partial class CharacterImageView : UserControl
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<CharacterImageView, IImage?>(nameof(Source));

    public static readonly StyledProperty<string?> CharacterNameProperty =
        AvaloniaProperty.Register<CharacterImageView, string?>(nameof(CharacterName));

    public static readonly StyledProperty<CardSpec?> SpecProperty =
        AvaloniaProperty.Register<CharacterImageView, CardSpec?>(nameof(Spec));

    public static readonly StyledProperty<bool> IsV1Property =
        AvaloniaProperty.Register<CharacterImageView, bool>(nameof(IsV1));

    public static readonly StyledProperty<bool> IsV2Property =
        AvaloniaProperty.Register<CharacterImageView, bool>(nameof(IsV2));

    public CharacterImageView() => InitializeComponent();

    /// <summary>The image to display; null shows the placeholder.</summary>
    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Name shown on the translucent ribbon over the image.</summary>
    public string? CharacterName
    {
        get => GetValue(CharacterNameProperty);
        set => SetValue(CharacterNameProperty, value);
    }

    /// <summary>Card spec that drives the corner badge; null hides it.</summary>
    public CardSpec? Spec
    {
        get => GetValue(SpecProperty);
        set => SetValue(SpecProperty, value);
    }

    // Derived from Spec so callers pass a single enum; kept as styled properties so the XAML
    // badge bindings observe their changes.
    public bool IsV1 => GetValue(IsV1Property);

    public bool IsV2 => GetValue(IsV2Property);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SpecProperty)
        {
            SetCurrentValue(IsV1Property, Spec == CardSpec.V1);
            SetCurrentValue(IsV2Property, Spec == CardSpec.V2);
        }
    }
}
