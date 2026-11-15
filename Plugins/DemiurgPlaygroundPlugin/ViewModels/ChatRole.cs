namespace DemiurgPlaygroundPlugin.ViewModels;

/// <summary>Who authored a transcript line; drives styling and alignment in the view.</summary>
public enum ChatRole
{
    /// <summary>A crew-member message.</summary>
    User,

    /// <summary>A reply spoken by the ship's computer directly.</summary>
    Computer,

    /// <summary>A reply from inside a running simulation.</summary>
    Simulation,
}
