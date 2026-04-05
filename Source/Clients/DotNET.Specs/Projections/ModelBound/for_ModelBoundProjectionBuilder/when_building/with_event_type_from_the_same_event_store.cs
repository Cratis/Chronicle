// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_event_type_from_the_same_event_store : Specification
{
    const string CurrentEventStore = "my-event-store";

    [EventType]
    [EventStore(CurrentEventStore)]
    class LocalEvent(string Name);

    [FromEvent<LocalEvent>]
    record ModelFromSameStore(
        [Key] Guid Id,
        string Name);

    ModelBoundProjectionBuilder _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        var namingPolicy = new TestNamingPolicy();
        var eventTypes = new EventTypesForSpecifications([typeof(LocalEvent)]);
        _builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes, CurrentEventStore);
    }

    void Because() => _result = _builder.Build(typeof(ModelFromSameStore));

    [Fact] void should_use_the_event_log_sequence() =>
        _result.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);
}
