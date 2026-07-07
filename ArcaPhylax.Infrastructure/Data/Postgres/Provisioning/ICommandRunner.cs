namespace ArcaPhylax.Data.Postgres.Provisioning;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default,
        bool throwOnNonZeroExit = false);
}
