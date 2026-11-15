using System.Collections.Immutable;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// Who may publish which plugin on this computer: endorsed publishers (built in or from the signed list)
/// for the ids they own, minus revoked keys; plus the user's overrides. A revocation beats everything; a
/// user's block beats an endorsement; a user-trusted key may not take over an id an endorsed publisher owns.
/// </summary>
public sealed record TrustPolicy(ImmutableList<Publisher> Endorsed, ImmutableList<string> RevokedKeyIds, UserTrust User)
{
    /// <summary>The policy for <paramref name="builtIn"/> publishers (compiled into the host; the signed <paramref name="list"/> can revoke them or change their prefixes) plus the <paramref name="user"/>'s overrides.</summary>
    public static TrustPolicy Of(IEnumerable<Publisher> builtIn, PublisherList list, UserTrust user)
        => new(
            builtIn
                .Where(publisher => !list.Publishers.Any(listed => listed.Key.KeyId == publisher.Key.KeyId))
                .Concat(list.Publishers)
                .Where(publisher => !list.RevokedKeyIds.Contains(publisher.Key.KeyId))
                .ToImmutableList(),
            list.RevokedKeyIds,
            user);

    /// <summary>Whether <paramref name="signer"/> may publish <paramref name="pluginId"/>, and why.</summary>
    public Either<TrustError, TrustedSigner> Check(string pluginId, PublisherKey signer)
        => (RevokedKeyIds.Contains(signer.KeyId), User.BlockedKeyIds.Contains(signer.KeyId)) switch
        {
            (true, _) => Distrusted(new PublisherRevoked(signer.KeyId)),
            (false, true) => Distrusted(new PublisherBlocked(signer.KeyId)),
            (false, false) => EndorsedOrUserTrusted(pluginId, signer),
        };

    /// <summary>Every publisher this policy knows, with its standing: endorsed, trusted by the user, or blocked.</summary>
    public ImmutableList<PublisherStanding> Standings()
        => Endorsed
            .Select(EndorsedStanding)
            .Concat(User.Trusted.Select(trusted => new UserTrustedPublisher(trusted)))
            .Concat(User.BlockedKeyIds
                .Where(keyId => !Endorsed.Any(publisher => publisher.Key.KeyId == keyId))
                .Select(keyId => new BlockedPublisher(keyId, Option<string>.None)))
            .ToImmutableList();

    private static Either<TrustError, TrustedSigner> Trusted(TrustedSigner signer) => Either<TrustError, TrustedSigner>.Right(signer);

    private static Either<TrustError, TrustedSigner> Distrusted(TrustError error) => Either<TrustError, TrustedSigner>.Left(error);

    private PublisherStanding EndorsedStanding(Publisher publisher)
        => User.BlockedKeyIds.Contains(publisher.Key.KeyId)
            ? new BlockedPublisher(publisher.Key.KeyId, Option.Some(publisher.Name))
            : new EndorsedPublisher(publisher);

    private Either<TrustError, TrustedSigner> EndorsedOrUserTrusted(string pluginId, PublisherKey signer)
        => Endorsed.FirstOrNone(publisher => publisher.Key == signer).Match(
            none: () => UserTrusted(pluginId, signer),
            some: publisher => publisher.Owns(pluginId)
                ? Trusted(new EndorsedSigner(publisher))
                : Distrusted(new NotOwner(pluginId, publisher.Name)));

    private Either<TrustError, TrustedSigner> UserTrusted(string pluginId, PublisherKey signer)
        => User.Trusted.FirstOrNone(trusted => trusted.Key == signer).Match(
            none: () => Distrusted(new UnknownPublisher(signer)),
            some: trusted => Endorsed.Any(publisher => publisher.Owns(pluginId))
                ? Distrusted(new NotOwner(pluginId, trusted.Name))
                : Trusted(new UserTrustedSigner(trusted)));
}
