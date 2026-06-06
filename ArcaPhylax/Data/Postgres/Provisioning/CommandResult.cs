namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed record CommandResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Succeeded => ExitCode == 0;
}
