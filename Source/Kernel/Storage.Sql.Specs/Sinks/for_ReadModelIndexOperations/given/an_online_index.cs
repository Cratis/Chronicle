// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ReadModelIndexOperations.given;

public abstract class an_online_index : Specification
{
    protected readonly Commands _commands = new();
    protected ISqlSinkHarness _harness;

    protected abstract ISqlSinkHarness CreateHarness();

    void Establish()
    {
        _harness = CreateHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"value":{"type":"integer"}}}""");
        _harness.CreateSink(new ReadModelDefinition("online", "online_shadows", "Online", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "online", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    protected async Task BuildAndDrop()
    {
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        builder.UseDatabaseFromConnectionString(_harness.ConnectionString);
        var options = builder.AddInterceptors(_commands).Options;
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), Substitute.For<ILogger<ReadModelMigrator>>());
        var columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"value":{"type":"integer"}}}"""));
        await using var context = new ReadModelDbContext(options, "online_shadows", columns, migrator);
        await context.EnsureTableExists();
        const string Name = "chronicle_shx_online_shadows_value";
        await ReadModelIndexOperations.Ensure(context, (context.TableName, "value"), Name, online: true);
        await ReadModelIndexOperations.Ensure(context, (context.TableName, "value"), Name, online: true);
        await ReadModelIndexOperations.Drop(context, context.TableName, Name, online: true);
    }

    void Destroy() => _harness.Dispose();

    protected sealed class Commands : DbCommandInterceptor
    {
        internal List<string> Sql { get; } = [];
        internal bool TransactionalIndex { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("CREATE INDEX", StringComparison.Ordinal) || command.CommandText.StartsWith("DROP INDEX", StringComparison.Ordinal))
            {
                Sql.Add(command.CommandText);
                TransactionalIndex |= command.Transaction is not null;
            }
            return ValueTask.FromResult(result);
        }
    }
}
