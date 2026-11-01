// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_read_model_table;

public class and_the_metadata_directory_cannot_be_listed : given.a_sqlite_database
{
    readonly ProjectedColumn[] _columns = [new("id", typeof(string), true, false, false, false)];
    readonly ReadModelTableHandle _ready = ReadModelTableHandle.For("store", "default", "customers", "customers", ReadModelTableRole.Live, "customers");
    readonly ReadModelTableHandle _new = ReadModelTableHandle.For("store", "default", "orders", "orders", ReadModelTableRole.Live, "orders");
    UnixFileMode _originalMode;
    Exception? _error;
    Exception? _readyError;
    ReadModelTableNameBinding? _newBinding;

    async Task Establish()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
            NullLogger<ReadModelMigrator>.Instance);
        _readModelMigrator.EnsureTableMigrated(Arg.Any<string>(), Arg.Any<ReadModelDbContext>())
            .Returns(call => migrator.EnsureTableMigrated(call.Arg<string>(), call.Arg<ReadModelDbContext>()));
        await using (var scope = await _database.Tables.Open(_ready, _columns))
        {
            (await scope.DbContext.Entries.CountAsync()).ShouldEqual(0);
        }

        _originalMode = File.GetUnixFileMode(_directory);
        File.SetUnixFileMode(_directory, UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var error = Catch.Exception(() => _ = Directory.EnumerateFileSystemEntries(_directory).ToArray());
        if (error is not UnauthorizedAccessException)
        {
            File.SetUnixFileMode(_directory, _originalMode);
            throw new InvalidOperationException("Directory enumeration must throw UnauthorizedAccessException; this permission spec cannot run as root.");
        }
    }

    async Task Because()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            _error = await Catch.Exception(async () =>
            {
                await using var scope = await _database.Tables.Open(_new, _columns);
            });
            _newBinding = await _database.Tables.Find(_new);
            _database.Tables.Clear();
            _readyError = await Catch.Exception(async () =>
            {
                await using var scope = await _database.Tables.Open(_ready, _columns);
                (await scope.DbContext.Entries.CountAsync()).ShouldEqual(0);
            });
        }
        finally
        {
            File.SetUnixFileMode(_directory, _originalMode);
        }
    }

    [Fact] void should_refuse_new_reservations_when_visibility_is_unknown()
    {
        if (!OperatingSystem.IsWindows()) _error.ShouldBeOfExactType<ReadModelTableMetadataUnavailable>();
    }

    [Fact] void should_not_leave_a_reservation() => _newBinding.ShouldBeNull();
    [Fact] void should_keep_serving_the_ready_binding() => _readyError.ShouldBeNull();
}
