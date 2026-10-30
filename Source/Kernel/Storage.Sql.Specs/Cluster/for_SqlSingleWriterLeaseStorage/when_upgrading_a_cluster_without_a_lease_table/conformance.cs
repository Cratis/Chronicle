// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_upgrading_a_cluster_without_a_lease_table;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    const string MigrationId = "Cluster-SingleWriterLease-v19_105_0";
    SingleWriterLeaseAttempt _result;
    string _existingBinding;
    bool _migrationRecorded;

    async Task Establish()
    {
        await using var context = Harness.CreateContext();
        var entity = context.Model.FindEntityType(typeof(SingleWriterLeaseRow))!;
        var removal = new MigrationBuilder(context.Database.ProviderName);
        removal.DropTable(entity.GetTableName()!, entity.GetSchema());

        // Recreate a pre-lease cluster: all preceding tables and history remain intact.
        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(removal.Operations, context.Model))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await context.Database.ExecuteSqlRawAsync(context.GetService<IHistoryRepository>().GetDeleteScript(MigrationId));
        context.DatabaseNames.Add(new DatabaseNameBinding { Kind = DatabaseKind.EventStore, EventStore = "existing", PhysicalName = "existing_physical" });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        await using var context = Harness.CreateContext();
        await context.Database.MigrateAsync();
        _existingBinding = await context.DatabaseNames.Where(binding => binding.EventStore == "existing").Select(binding => binding.PhysicalName).SingleAsync();
        _migrationRecorded = (await context.Database.GetAppliedMigrationsAsync()).Contains(MigrationId);
        _result = await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    [Fact] void should_create_a_usable_lease_table() => _result.Acquired.ShouldBeTrue();
    [Fact] void should_preserve_the_existing_binding() => _existingBinding.ShouldEqual("existing_physical");
    [Fact] void should_record_the_additive_migration() => _migrationRecorded.ShouldBeTrue();
}
