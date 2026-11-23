// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_a_reserved_destination_is_requested : Specification
{
    readonly List<Exception?> _errors = [];
    IDatabase _database;
    ReadModelDefinition _definition;

    void Establish()
    {
        _database = Substitute.For<IDatabase>();
        _definition = new("model", "ordinary", "Model", ReadModelOwner.Client, ReadModelSource.Code,
            ReadModelObserverType.NotSet, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"}}}""") }, []);
    }

    async Task Because()
    {
        var sink = Create(_definition);
        _database.ClearReceivedCalls();
        _errors.Add(Catch.Exception(() => Create(_definition with { ContainerName = SinkProvenanceNames.Metadata.ToUpperInvariant() })));
        _errors.Add(await Catch.Exception(() => sink.Remove(SinkProvenanceNames.Metadata)));
        _errors.Add(await Catch.Exception(() => sink.EndReplay(new ReplayContext(new(_definition.Identifier, 1), _definition.ContainerName, SinkProvenanceNames.Metadata, DateTimeOffset.UnixEpoch))));
    }

    Sink Create(ReadModelDefinition definition) => new("store", "namespace", definition, _database, Substitute.For<IExpandoObjectConverter>(), new ReplayingTables(), NullLogger<Sink>.Instance);

    [Fact] void should_refuse_construction_remove_and_revert_before_table_access() => _errors.TrueForAll(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_never_resolve_probe_or_provision_a_table() => _database.ReceivedCalls().Any().ShouldBeFalse();
}
