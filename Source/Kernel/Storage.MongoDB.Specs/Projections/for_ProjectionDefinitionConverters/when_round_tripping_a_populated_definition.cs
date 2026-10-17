// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;
using KernelDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;
using StoredDefinition = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_a_populated_definition : given.a_populated_definition
{
    KernelDefinition _restored;
    ProjectionDefinitionCompareResult _comparison;
    StoredDefinition _stored;

    async Task Because()
    {
        _stored = BsonSerializer.Deserialize<StoredDefinition>(_definition.ToMongoDB().ToBsonDocument());
        _restored = _stored.ToKernel();
        var storage = Substitute.For<IStorage>();
        var definitions = Substitute.For<IProjectionDefinitionsStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Projections.Returns(definitions);
        definitions.Has(Arg.Any<ProjectionId>()).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        _comparison = await comparer.Compare(new(_definition.Identifier, "store"), _definition, _restored);
    }

    [Fact] void should_compare_as_unchanged() => _comparison.ShouldEqual(ProjectionDefinitionCompareResult.Same);
    [Fact] void should_write_empty_tags_for_older_readers() => _stored.Tags.ShouldBeEmpty();
    [Fact] void should_write_empty_auto_map_exclusions_for_older_readers() => _stored.NoAutoMapProperties.ShouldBeEmpty();
    [Fact] void should_write_empty_child_collections_for_older_readers() => _stored.Children.Values.Single().Nested.ShouldBeEmpty();
    [Fact] void should_write_empty_child_auto_map_exclusions_for_older_readers() => _stored.Children.Values.Single().NoAutoMapProperties.ShouldBeEmpty();
    [Fact] void should_write_empty_nested_collections_for_older_readers() => _stored.Nested.Values.Single().Nested.ShouldBeEmpty();
    [Fact] void should_write_empty_nested_auto_map_exclusions_for_older_readers() => _stored.Nested.Values.Single().NoAutoMapProperties.ShouldBeEmpty();
    [Fact] void should_retain_the_declared_every_event_key() => _restored.FromEvery.Key.ShouldEqual(_definition.FromEvery.Key);
    [Fact] void should_retain_the_declared_every_event_auto_map_setting() => _restored.FromEvery.AutoMap.ShouldEqual(_definition.FromEvery.AutoMap);
}
