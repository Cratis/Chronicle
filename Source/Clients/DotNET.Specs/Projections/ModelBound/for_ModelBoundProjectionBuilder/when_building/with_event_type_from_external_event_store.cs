// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_event_type_from_external_event_store : Specification
{
    const string SourceEventStore = "source-event-store";

    [EventType]
    [EventStore(SourceEventStore)]
    class ExternalEvent(string Name);

    [FromEvent<ExternalEvent>]
    record ModelFromExternalStore(
        [Key] Guid Id,
        string Name);

    ModelBoundProjectionBuilder _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        var namingPolicy = new TestNamingPolicy();
        var eventTypes = new EventTypesForSpecifications([typeof(ExternalEvent)]);
        _builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes);
    }

    void Because() => _result = _builder.Build(typeof(ModelFromExternalStore));

    [Fact] void should_use_the_inbox_event_sequence_for_the_source_event_store() =>
        _result.EventSequenceId.ShouldEqual(new EventSequenceId($"{EventSequenceId.InboxPrefix}{SourceEventStore}").Value);
}
