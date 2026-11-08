// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings.for_MaterializationBindingDocuments;

public class when_round_tripping_the_sink_of_a_key : Specification
{
    MaterializationKey _default;
    MaterializationKey _named;
    BsonDocument _defaultDocument;
    BsonDocument _namedDocument;
    MaterializationBinding _defaultBinding;
    MaterializationBinding _namedBinding;

    void Establish()
    {
        _default = new(new(Guid.Parse("fa5dc415-e617-48a0-9975-fd1580aa321a")), "tenant", "projection", "MongoDB", SinkConfigurationId.None);
        _named = _default with { SinkName = "reporting" };
    }

    void Because()
    {
        var record = new MaterializationGenerationRecord(MaterializationGenerationIdentity.For(new ProjectionId("projection"), MaterializationGeneration.First, "models"), MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch);
        _defaultDocument = MaterializationBindingDocuments.First(_default, record);
        _namedDocument = MaterializationBindingDocuments.First(_named, record);
        _defaultBinding = MaterializationBindingDocuments.Binding(_defaultDocument);
        _namedBinding = MaterializationBindingDocuments.Binding(_namedDocument);
    }

    [Fact] void should_not_store_a_name_for_the_implicit_default_sink() => _defaultDocument.Contains("sinkName").ShouldBeFalse();
    [Fact] void should_store_the_name_of_a_named_sink() => _namedDocument["sinkName"].AsString.ShouldEqual("reporting");
    [Fact] void should_read_the_implicit_default_sink_back() => _defaultBinding.Key.ShouldEqual(_default);
    [Fact] void should_read_the_named_sink_back() => _namedBinding.Key.ShouldEqual(_named);
    [Fact] void should_store_different_identities() => _namedDocument["_id"].AsString.ShouldNotEqual(_defaultDocument["_id"].AsString);
}
