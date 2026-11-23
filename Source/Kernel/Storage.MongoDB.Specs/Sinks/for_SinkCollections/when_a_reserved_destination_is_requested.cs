// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections;

public class when_a_reserved_destination_is_requested : Specification
{
    readonly List<Exception?> _errors = [];
    IMongoDatabase _database;
    ReadModelDefinition _definition;

    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _definition = new(
            "model",
            "ordinary",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.NotSet,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"}}}""") },
            []);
    }

    async Task Because()
    {
        var collections = new SinkCollections(_definition, _database);
        _database.ClearReceivedCalls();
        _errors.Add(Catch.Exception(() => _ = new SinkCollections(_definition with { ContainerName = SinkProvenanceNames.Metadata.ToUpperInvariant() }, _database)));
        _errors.Add(Catch.Exception(() => collections.GetCollection(SinkProvenanceNames.Metadata)));
        _errors.Add(await Catch.Exception(() => collections.Remove(SinkProvenanceNames.Metadata)));
        _errors.Add(await Catch.Exception(() => collections.EndReplay(new ReplayContext(new(_definition.Identifier, 1), _definition.ContainerName, SinkProvenanceNames.Metadata, DateTimeOffset.UnixEpoch))));
    }

    [Fact] void should_refuse_constructor_explicit_lookup_remove_and_revert_destinations() => _errors.TrueForAll(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_never_resolve_query_drop_or_rename_a_collection() => _database.ReceivedCalls().Any().ShouldBeFalse();
}
