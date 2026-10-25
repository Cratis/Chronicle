// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(PostgreSqlCollection.Name)]
public class and_a_postgresql_index_is_invalid(PostgreSqlFixture fixture) : Specification
{
    PostgreSqlSinkHarness _harness;
    ReadModelDbContext _context;
    string _name;
    bool _wasInvalid;
    bool _isValid;

    async Task Establish()
    {
        _harness = new PostgreSqlSinkHarness { Fixture = fixture };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"current":{"type":"integer"}}}""");
        var definition = new ReadModelDefinition("repair", "index_repair", "Repair", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "repair", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new IndexDefinition("current")]);
        await _harness.CreateSink(definition).EnsureIndexes();
        var options = SpecDbContextOptions.Kernel<ReadModelDbContext>(_harness.ConnectionString);
        _context = new ReadModelDbContext(options, "index_repair", ProjectedColumns.ForSchema(schema), Substitute.For<IReadModelMigrator>());
        _name = ReadModelIndexNames.For(Cratis.Arc.EntityFrameworkCore.DatabaseType.PostgreSql, "index_repair", "current");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO index_repair (id, current) VALUES ('one', 1), ('two', 1)");
#pragma warning disable EF1002 // The generated identifier contains only the fixed Chronicle prefix and a hexadecimal digest.
        await _context.Database.ExecuteSqlRawAsync($"DROP INDEX \"{_name}\"");
        try
        {
            await _context.Database.ExecuteSqlRawAsync($"CREATE UNIQUE INDEX CONCURRENTLY \"{_name}\" ON index_repair (current)");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _wasInvalid = !await IsValid();
        }
#pragma warning restore EF1002
    }

    async Task Because()
    {
        await ReadModelIndexes.Ensure(_context, "index_repair", "current", true);
        _isValid = await IsValid();
    }

    Task<bool> IsValid() => _context.Database.SqlQuery<bool>($"SELECT i.indisvalid AS \"Value\" FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid WHERE c.relname={_name} AND c.relnamespace=(SELECT oid FROM pg_namespace WHERE nspname=current_schema())").SingleAsync();

    void Destroy()
    {
        _context.Dispose();
        _harness.Dispose();
    }

    [Fact] void should_have_an_invalid_index_left_by_the_failed_concurrent_build() => _wasInvalid.ShouldBeTrue();
    [Fact] void should_replace_it_with_a_valid_index_instead_of_accepting_its_name() => _isValid.ShouldBeTrue();
}
