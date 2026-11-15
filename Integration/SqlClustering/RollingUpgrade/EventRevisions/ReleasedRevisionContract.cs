// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

/// <summary>
/// Exercises the released SQL writer against a table migrated by the new provider.
/// </summary>
public abstract class ReleasedRevisionContract : Specification
{
    /// <summary>Gets the pinned released image.</summary>
    protected abstract string Image { get; }

    /// <summary>Gets whether to use PostgreSQL instead of SQL Server.</summary>
    protected abstract bool PostgreSql { get; }

    [Fact]
    public async Task should_roundtrip_revisions_and_redactions_in_migrate_and_verify_modes()
    {
        await using var kernel = new ReleasedRevisionKernel(Image, PostgreSql);
        await kernel.Start();
        const string witness = "released-revision-bootstrap";
        using (var bootstrap = Client(kernel))
        {
            var store = await bootstrap.GetEventStore("released-revisions");
            (await store.EventLog.Append(witness, new ClusterEventAppended(Guid.NewGuid()))).IsSuccess.ShouldBeTrue();
        }
        await kernel.Stop();
        var target = await ReleasedSequenceDatabase.Find(kernel.Database, PostgreSql, witness);
        await kernel.PrepareSchema();
        await new NewNodeStorage(target.Connection, target.Table, PostgreSql).EnsureTableExists();
        foreach (var mode in new[] { "Migrate", "Verify" })
        {
            var table = $"revision-created-{mode.ToLowerInvariant()}";
            var storage = new NewNodeStorage(target.Connection, table, PostgreSql);
            (await storage.TableExists()).ShouldBeFalse();
            await storage.EnsureTableExists();
            (await storage.TableExists()).ShouldBeTrue();
            await kernel.Restart(mode);
            using var client = Client(kernel);
            var store = await client.GetEventStore("released-revisions");
            await new ReleasedRevisionScenario(store.GetEventSequence(table), storage).Run();
            await kernel.Stop();
            await storage.EnsureTableExists();
        }
    }

    static ChronicleClient Client(ReleasedRevisionKernel kernel)
    {
        var options = new ChronicleOptions(kernel.ConnectionString) { ConnectTimeout = 30 };
        options.ExplicitArtifacts.RegisterEventType<ClusterEventAppended>();
        return new(options);
    }
}
