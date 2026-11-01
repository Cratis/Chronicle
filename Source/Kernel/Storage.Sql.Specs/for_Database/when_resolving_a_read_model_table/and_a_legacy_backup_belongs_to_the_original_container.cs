// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_read_model_table;

public class and_a_legacy_backup_belongs_to_the_original_container : given.a_sqlite_read_model_table
{
    ReadModelTableNameBinding? _binding;

    async Task Establish()
    {
        await using (var scope = await ReadModelTable())
        {
            await scope.DbContext.Database.ExecuteSqlRawAsync("CREATE TABLE \"old-backup\" (\"Id\" TEXT NOT NULL PRIMARY KEY)");
        }

        await using var history = await _database.Namespace("store", "namespace");
        history.DbContext.ReplayedModels.Add(new ReplayedModelOccurrence
        {
            ObserverId = "observer",
            ReadModelIdentifier = "model",
            ReadModelName = "old-container",
            RevertModelName = "old-backup",
            Started = DateTimeOffset.UnixEpoch
        });
        await history.DbContext.SaveChangesAsync();
    }

    async Task Because() => _binding = await _database.Tables.FindBackup(
        ReadModelTableHandle.For("store", "namespace", "model", "renamed-container", ReadModelTableRole.Backup, "old-backup"));

    [Fact] void should_recover_the_original_container() => _binding!.Owner.ContainerName.ShouldEqual("old-container");
    [Fact] void should_adopt_the_existing_backup() => _binding!.PhysicalName.ShouldEqual("old-backup");
    [Fact] void should_commit_a_ready_binding() => _binding!.State.ShouldEqual(ReadModelTableBindingState.Ready);
}
