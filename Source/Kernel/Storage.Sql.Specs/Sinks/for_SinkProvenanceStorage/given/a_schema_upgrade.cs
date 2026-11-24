// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage.given;

public abstract class a_schema_upgrade : Specification
{
    protected readonly SchemaUpgradeCommands _commands = new();
    protected Exception? _error;
    protected Exception? _corruptError;
    protected Exception? _providerCorruptError;
    protected bool _missingClean;
    protected string[] _readStatements;
    protected bool _initiallyClean;
    protected bool _restricted;
    protected bool _retained;

    protected async Task Exercise(bool sqlServer)
    {
        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>(), Arg.Any<SinkProvenanceTarget?>())
            .Returns(call =>
            {
                var options = new DbContextOptionsBuilder<ReadModelDbContext>();
                if (sqlServer) options.UseSqlServer("Server=localhost;Database=unit;User Id=unit;Password=unit;Encrypt=False");
                else options.UseNpgsql("Host=localhost;Database=unit;Username=unit;Password=unit");
                options.AddInterceptors(_commands);
                var context = Substitute.ForPartsOf<ReadModelDbContext>(options.Options, call.ArgAt<string>(2), call.ArgAt<IReadOnlyList<ProjectedColumn>>(3), Substitute.For<IReadModelMigrator>(), "custom");
                context.Configure().SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
                {
                    _commands.Imported = true;
                    return Task.FromResult(1);
                });
                return Task.FromResult(new DbContextScope<ReadModelDbContext>(context, static () => { }));
            });
        var storage = new SinkProvenanceStorage(database, "store", "namespace");
        _commands.Missing = true;
        _missingClean = !(await storage.ReadState()).Restricted;
        _commands.Missing = false;
        _commands.Legacy = false;
        _initiallyClean = !(await storage.ReadState()).Restricted;
        _commands.Legacy = true;
        _commands.Readable = false;
        _error = await Catch.Exception(storage.ReadState);
        _commands.Readable = true;
        _commands.Corrupt = true;
        _providerCorruptError = await Catch.Exception(storage.ReadState);
        _commands.Corrupt = false;
        _commands.Version = 99;
        _corruptError = await Catch.Exception(storage.ReadState);
        _commands.Version = 1;
        _readStatements = _commands.Statements.ToArray();
        _restricted = (await storage.CompleteInventory(false)).Restricted;
        _commands.Legacy = false;
        _retained = (await new SinkProvenanceStorage(database, "store", "namespace").ReadState()).Restricted;
    }
}
