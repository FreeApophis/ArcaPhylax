using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>
/// The current <see cref="TrustPolicy"/>: built-in publishers, the newest root-signed publisher list seen so
/// far (cached on disk), and the user's own overrides. An older publisher list is never accepted, so a
/// revoked publisher cannot come back by serving a stale copy.
/// </summary>
/// <remarks>
/// The user's overrides live in a plain file in their profile. That adds no attack surface: anything that
/// can write there can already replace installed plugin files directly.
/// </remarks>
public sealed class PluginTrust
{
    private readonly string _publishersPath;
    private readonly string _userTrustPath;

    public PluginTrust(string dataRoot)
    {
        _publishersPath = Path.Combine(dataRoot, PublisherList.FileName);
        _userTrustPath = Path.Combine(dataRoot, UserTrust.FileName);
        Publishers = TextOf(_publishersPath)
            .SelectMany(json => PublisherList.Verify(json, TrustAnchors.Root).Match(left: _ => Option<PublisherList>.None, right: Option.Some))
            .GetOrElse(PublisherList.Empty);
        User = TextOf(_userTrustPath).Match(none: UserTrust.Empty, some: UserTrust.Parse);
    }

    /// <summary>Raised after the policy changed (new publisher list or a user override).</summary>
    public event EventHandler? Changed;

    public PublisherList Publishers { get; private set; }

    public UserTrust User { get; private set; }

    public TrustPolicy Policy => TrustPolicy.Of(TrustAnchors.BuiltIn, Publishers, User);

    /// <summary>
    /// Takes a downloaded publishers.json if the root key signed it and it is newer than the current one.
    /// Returns the list now in effect, or why the download was refused.
    /// </summary>
    public Either<string, PublisherList> Accept(string signedJson)
        => PublisherList.Verify(signedJson, TrustAnchors.Root).SelectMany(list => list.Sequence switch
        {
            var sequence when sequence > Publishers.Sequence => Either<string, PublisherList>.Right(Saved(list, signedJson)),
            var sequence when sequence == Publishers.Sequence => Either<string, PublisherList>.Right(Publishers),
            _ => Either<string, PublisherList>.Left($"the publisher list is older ({list.Sequence}) than the one already known ({Publishers.Sequence})."),
        });

    public void TrustPublisher(string name, PublisherKey key) => ChangeUser(User.Trust(name, key));

    public void BlockPublisher(string keyId) => ChangeUser(User.Block(keyId));

    public void ForgetPublisher(string keyId) => ChangeUser(User.Forget(keyId));

    private static Option<string> TextOf(string path)
        => File.Exists(path) ? File.ReadAllText(path) : Option<string>.None;

    private PublisherList Saved(PublisherList list, string signedJson)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_publishersPath)!);
        File.WriteAllText(_publishersPath, signedJson);
        Publishers = list;
        Changed?.Invoke(this, EventArgs.Empty);
        return list;
    }

    private void ChangeUser(UserTrust user)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_userTrustPath)!);
        File.WriteAllText(_userTrustPath, user.ToJson());
        User = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
