// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_reporting_multiple_registered_read_models : for_SchemaProvisioner.given.a_schema_provisioner
{
    readonly given.SchemaLogger<DynamicSchemaGuard> _logger = new();
    ServiceProvider _runtimeServices;
    Database _runtime;
    int _warningsBeforeFlush;
    int _firstLogCount;
    int _secondLogCount;

    async Task Establish()
    {
        await _provisioner.Migrate(Scope);
        await using var registry = await _schemaDatabase.EventStore("orders");
        registry.DbContext.ReadModels.Add(for_SchemaProvisioner.given.RegisteredDynamicSchema.Definition);
        var second = for_SchemaProvisioner.given.RegisteredDynamicSchema.Definition;
        second.Id = "second-model";
        second.Name = "second_model";
        registry.DbContext.ReadModels.Add(second);
        await registry.DbContext.SaveChangesAsync();
        var options = new ChronicleOptions
        {
            Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = _directory, SchemaMode = SchemaMode.Verify }
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(options));
        SqlSchemaServices.Add(services, options);
        services.AddSingleton<ILogger<DynamicSchemaGuard>>(_logger);
        _runtimeServices = services.BuildServiceProvider();
        _runtime = _runtimeServices.GetRequiredService<Database>();
    }

    async Task Because()
    {
        using (_runtimeServices.GetRequiredService<DynamicSchemaGuard>().FlushOnDispose())
        {
            await AccessRegistrations();
            _warningsBeforeFlush = _logger.Entries.Count(entry => entry.Level == LogLevel.Warning);
        }
        _firstLogCount = _logger.Entries.Count;
        await AccessRegistrations();
        _secondLogCount = _logger.Entries.Count;
    }

    async Task AccessRegistrations()
    {
        await using var registry = await _runtime.EventStore("orders");
        foreach (var stored in await registry.DbContext.ReadModels.AsNoTracking().ToListAsync())
        {
            var definition = stored.ToKernel();
            var columns = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration(), definition.Indexes);
            await using var table = await _runtime.ReadModelTable("orders", "production", stored.Name, columns);
        }
    }

    async Task Destroy() => await _runtimeServices.DisposeAsync();

    [Fact] void should_defer_the_summary_until_the_group_is_flushed() => _warningsBeforeFlush.ShouldEqual(0);
    [Fact] void should_emit_one_warning_for_both_registrations() => _logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_include_the_number_of_reported_objects() => Convert.ToInt32(_logger.Entries.Single(entry => entry.Level == LogLevel.Warning).State["Count"]).ShouldEqual(_logger.Entries.Count(entry => entry.Level == LogLevel.Debug));
    [Fact] void should_detail_each_table_and_its_indexes() => _logger.Entries.Count(entry => entry.Level == LogLevel.Debug).ShouldEqual(6);
    [Fact] void should_not_report_the_same_objects_again() => _secondLogCount.ShouldEqual(_firstLogCount);
}
