// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_building;

public class a_node_with_event_source_id_mappings : given.a_graph_definition_builder
{
    GraphNodeProjectionDefinition _result;

    void Because() => _result = _builder.Build(ModelBoundModelOf(typeof(Person)), "people", "Neo4j").Projection.Graph!.Nodes.Single().Value;

    [Fact] void should_map_the_parameter_to_the_event_source_id() => _result.From.Single().Value.Properties["source"].ShouldEqual("$eventSourceId");
    [Fact] void should_support_setting_a_property_once() => _result.From.Single().Value.Properties["originalSource"].ShouldEqual("$setOnce($eventSourceId)");
    [Fact] void should_preserve_the_existing_context_mapping() => _result.From.Single().Value.Properties["legacySource"].ShouldEqual("$eventContext(occurred)");

    [Node]
    [FromEvent<given.PersonRegistered>]
    record Person(
        [Key] Guid Id,
        [SetFromEventSourceId<given.PersonRegistered>] EventSourceId Source,
        [property: SetFromEventSourceId<given.PersonRegistered>(SetOnce = true)] EventSourceId OriginalSource,
        [SetFromContext<given.PersonRegistered>(nameof(EventContext.Occurred))] DateTimeOffset LegacySource);
}
