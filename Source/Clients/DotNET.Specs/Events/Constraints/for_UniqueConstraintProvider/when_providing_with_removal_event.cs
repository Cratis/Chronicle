// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Serialization;
using NJsonSchema;

namespace Cratis.Chronicle.Events.Constraints;

public class when_providing_with_removal_event : Specification
{
    const string ConstraintName = "MyConstraint";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    EventType _eventType;
    EventType _removalEventType;
    JsonSchema _eventSchema;
    INamingPolicy _namingPolicy;
    UniqueConstraintProvider _provider;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _eventType = new EventType(nameof(EventWithConstraint), EventTypeGeneration.First);
        _eventSchema = JsonSchema.FromType<EventWithConstraint>();
        _eventTypes.GetEventTypeFor(typeof(EventWithConstraint)).Returns(_eventType);
        _eventTypes.GetSchemaFor(_eventType.Id).Returns(_eventSchema);

        _removalEventType = new EventType(nameof(ConstraintRemovalEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(ConstraintRemovalEvent)).Returns(_removalEventType);

        _clientArtifactsProvider.UniqueConstraints.Returns([typeof(EventWithConstraint)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([typeof(ConstraintRemovalEvent)]);

        _namingPolicy = new DefaultNamingPolicy();
        _provider = new UniqueConstraintProvider(_clientArtifactsProvider, _eventTypes, _namingPolicy);
    }

    void Because() => _result = _provider.Provide();

    [Fact] void should_return_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_set_removed_with_to_removal_event() => ((UniqueConstraintDefinition)_result[0]).RemovedWith.ShouldEqual(_removalEventType.Id);

    record EventWithConstraint([property: Unique(ConstraintName)] string Property);
    [RemoveConstraint(ConstraintName)] record ConstraintRemovalEvent;
}
