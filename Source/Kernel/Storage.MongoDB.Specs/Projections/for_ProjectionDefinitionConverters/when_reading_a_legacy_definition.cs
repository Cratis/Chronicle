// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;
using KernelDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;
using StoredDefinition = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_reading_a_legacy_definition : given.a_populated_definition
{
    KernelDefinition _restored;

    void Because()
    {
        var document = _definition.ToMongoDB().ToBsonDocument();
        document.Remove("scope");
        document.Remove("storageFormatVersion");
        document["fromEvery"].AsBsonDocument.Remove("key");
        _restored = BsonSerializer.Deserialize<StoredDefinition>(document).ToKernel();
    }

    [Fact] void should_identify_the_fields_lost_by_the_previous_storage_format() => _restored.HasLegacyStorageFields.ShouldBeTrue();
    [Fact] void should_default_to_namespaced_scope() => _restored.Scope.ShouldEqual(ProjectionScope.Namespaced);
    [Fact] void should_default_to_event_source_key() => _restored.FromEvery.Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
}
