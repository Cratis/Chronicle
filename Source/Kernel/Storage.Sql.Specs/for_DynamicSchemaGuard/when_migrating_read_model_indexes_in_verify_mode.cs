// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_migrating_read_model_indexes_in_verify_mode : EventStores.Namespaces.ReadModels.for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly ILogger<DynamicSchemaGuard> _logger = Substitute.For<ILogger<DynamicSchemaGuard>>();
    ReadModelDbContext _context;
    bool _shadowMissing;
    bool _markerMissing;
    int _logsAfterFirst;
    int _logsAfterSecond;

    async Task Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        await using var original = CreateContext("guarded_read_model", ProjectedColumns.ForSchema(schema));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>((DbContextOptions<ReadModelDbContext>)original.GetService<IDbContextOptions>());
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new SchemaModeOptionsExtension(SchemaMode.Verify, new DynamicSchemaGuard(_logger)));
        _context = new ReadModelDbContext(options.Options, original.TableName, original.Columns, _migrator);
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _shadowMissing = await ShadowColumnIndexes.NeedsReconciliation(_context);
        _markerMissing = await InitializedMarkerIndex.NeedsIndex(_context);
        _logsAfterFirst = _logger.ReceivedCalls().Count();
        _migrator.ClearMigrationCache(_context.Database.GetConnectionString()!);
        await _context.EnsureTableExists();
        _logsAfterSecond = _logger.ReceivedCalls().Count();
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_create_shadow_indexes() => _shadowMissing.ShouldBeFalse();
    [Fact] void should_create_the_initialized_marker_index() => _markerMissing.ShouldBeFalse();
    [Fact] void should_record_the_shadow_index_ddl() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument?.ToString()?.Contains("chronicle_shx_", StringComparison.Ordinal) == true)).ShouldBeTrue();
    [Fact] void should_record_the_marker_index_ddl() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument?.ToString()?.Contains("chronicle_imx_", StringComparison.Ordinal) == true)).ShouldBeTrue();
    [Fact] void should_not_call_the_guard_for_current_indexes() => _logsAfterSecond.ShouldEqual(_logsAfterFirst);
}
