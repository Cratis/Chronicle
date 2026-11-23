// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StorageSinks = Cratis.Chronicle.Storage.Sinks.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_named_sinks_share_a_database : Specification
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-provenance-routing-{Guid.NewGuid():N}");
    readonly List<Exception?> _errors = [];
    ServiceProvider _services;
    StorageSinks _sinks;
    ReadModelDefinition _model;
    bool _alias;
    bool _restarted;
    bool _independent;

    void Establish()
    {
        Directory.CreateDirectory(_directory);
        var primary = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "primary.db"), Pooling = false };
        var alias = new SqliteConnectionStringBuilder(primary.ConnectionString) { Pooling = true };
        var independent = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "independent.db"), Pooling = false };
        var options = new ChronicleOptions
        {
            Storage = new() { Type = StorageType.MongoDB, ConnectionDetails = "mongodb://localhost:27017" },
            Sinks = new()
            {
                ReadModels = "Primary",
                Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Primary"] = new() { Type = "SQL", ConnectionDetails = primary.ConnectionString },
                    ["Alias"] = new() { Type = "SQL", ConnectionDetails = alias.ConnectionString },
                    ["Independent"] = new() { Type = "SQL", ConnectionDetails = independent.ConnectionString }
                }
            }
        };
        _services = new ServiceCollection().AddLogging().AddSingleton(Options.Create(options)).BuildServiceProvider();
        var factory = new SinkFactory(_services, new ExpandoObjectConverter(new TypeFormats()));
        _sinks = new("store", "namespace", new KnownInstancesOf<ISinkFactory>([factory]), new SinkConfigurations(Options.Create(options)));
        _model = new(
            "model",
            "model",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.NotSet,
            ReadModelObserverIdentifier.Unspecified,
            new(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
            []);
    }

    async Task Because()
    {
        await _sinks.GetProvenance(_model).MarkQuarantined();
        var aliasModel = _model with { Identifier = "alias", Sink = _model.Sink with { Type = WellKnownSinkTypes.SQL, Name = "Alias" } };
        _alias = await _sinks.GetProvenance(aliasModel).IsQuarantined();
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(_model)));
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(aliasModel)));
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(aliasModel with { Owner = ReadModelOwner.Server, ContainerName = "renamed" })));
        _independent = await _sinks.GetProvenance(_model with { Sink = _model.Sink with { Name = "Independent" } }).IsQuarantined();
        var restarted = new SinkFactory(_services, new ExpandoObjectConverter(new TypeFormats()));
        _restarted = await restarted.GetProvenance("store", "namespace", "Alias").IsQuarantined();
    }

    [Fact] void should_mark_the_configured_default_through_the_resolved_sql_provider() => _alias.ShouldBeTrue();
    [Fact] void should_refuse_default_alias_and_relabelled_destinations() => _errors.TrueForAll(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_retain_the_mark_in_a_new_factory_and_database_instance() => _restarted.ShouldBeTrue();
    [Fact] void should_leave_a_separate_named_database_unmarked() => _independent.ShouldBeFalse();

    async Task Destroy()
    {
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }
}
