using ArcaPhylax.Infrastructure.Migrations;
using ArcaPhylax.Infrastructure.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.Infrastructure.Tests;

public sealed class MigrationRunnerTests
{
    [Fact]
    public void RunCore_OnFreshDatabase_CreatesSchemaAndRecordsBaseline()
    {
        // Arrange
        using var db = new TempDatabase();

        // Act
        db.Runner().RunCore();

        // Assert
        var itemsTable = db.Scalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='items';");
        var baseline = db.Scalar(
            "SELECT COUNT(*) FROM schema_migrations WHERE module='core' AND version=1;");
        Assert.Equal(1, itemsTable);
        Assert.Equal(1, baseline);
    }

    [Fact]
    public void RunCore_WhenRunTwice_IsIdempotentAndRecordsBaselineOnce()
    {
        // Arrange
        using var db = new TempDatabase();
        db.Runner().RunCore();

        // Act
        db.Runner().RunCore();

        // Assert
        var baselineRows = db.Scalar(
            "SELECT COUNT(*) FROM schema_migrations WHERE module='core' AND version=1;");
        Assert.Equal(1, baselineRows);
    }

    [Fact]
    public void RunCore_DoesNotSeedTheLegacyDocumentsTable()
    {
        // Arrange
        using var db = new TempDatabase();

        // Act
        db.Runner().RunCore();

        // Assert
        Assert.Equal(0, db.Scalar("SELECT COUNT(*) FROM Documents;"));
    }

    [Fact]
    public void Create_EnablesForeignKeyEnforcement()
    {
        // Arrange
        using var db = new TempDatabase();

        // Act
        var foreignKeys = db.Scalar("PRAGMA foreign_keys;");

        // Assert
        Assert.Equal(1, foreignKeys);
    }

    [Fact]
    public void Run_WritesAPreMigrateBackupFileBeforeApplying()
    {
        // Arrange
        using var db = new TempDatabase();

        // Act
        db.Runner().RunCore();

        // Assert
        var backups = Directory.Exists(db.BackupsDirectory)
            ? Directory.GetFiles(db.BackupsDirectory, "pre-migrate-core-*.db")
            : [];
        Assert.NotEmpty(backups);
    }

    [Fact]
    public void Run_WhenAnAlreadyAppliedScriptIsEdited_Throws()
    {
        // Arrange
        using var db = new TempDatabase();
        var runner = db.Runner();
        runner.Run("test", [new MigrationScript(1, "one", "CREATE TABLE t1(x);", "checksum-A")]);
        var edited = new MigrationScript(1, "one", "CREATE TABLE t1(x);", "checksum-B");

        // Act
        var act = () => runner.Run("test", [edited]);

        // Assert
        Assert.Throws<MigrationException>(act);
    }

    [Fact]
    public void Run_WhenAScriptFails_RollsBackThatScriptButKeepsEarlierOnes()
    {
        // Arrange
        using var db = new TempDatabase();
        var runner = db.Runner();
        var good = new MigrationScript(1, "good", "CREATE TABLE good(x);", "c1");
        var broken = new MigrationScript(2, "broken", "THIS IS NOT VALID SQL;", "c2");

        // Act
        var act = () => runner.Run("test", [good, broken]);

        // Assert
        Assert.Throws<MigrationException>(act);
        Assert.Equal(1, db.Scalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='good';"));
        Assert.Equal(1, db.Scalar(
            "SELECT COUNT(*) FROM schema_migrations WHERE module='test' AND version=1;"));
        Assert.Equal(0, db.Scalar(
            "SELECT COUNT(*) FROM schema_migrations WHERE module='test' AND version=2;"));
    }
}
