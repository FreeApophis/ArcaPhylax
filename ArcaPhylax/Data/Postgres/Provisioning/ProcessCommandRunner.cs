using System.Diagnostics;

namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class ProcessCommandRunner() : ICommandRunner
{
    public async Task<CommandResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default,
        bool throwOnNonZeroExit = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            if (throwOnNonZeroExit)
            {
                throw;
            }

            return new CommandResult(-1, string.Empty, ex.Message);
        }

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdOut = await stdOutTask.ConfigureAwait(false);
        var stdErr = await stdErrTask.ConfigureAwait(false);
        var result = new CommandResult(process.ExitCode, stdOut, stdErr);

        if (throwOnNonZeroExit && !result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Command failed: {fileName} {arguments}{Environment.NewLine}" +
                $"Exit code: {result.ExitCode}{Environment.NewLine}" +
                $"STDOUT: {result.StdOut}{Environment.NewLine}" +
                $"STDERR: {result.StdErr}");
        }

        return result;
    }
}
