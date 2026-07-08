namespace ArcaPhylax.Infrastructure.Migrations;

public sealed class MigrationException(string message, Exception? inner = null)
    : Exception(message, inner);
