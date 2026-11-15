// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_a_schema_violation_occurs : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result = null!;

    void Establish() => _appendResultFor = _ => AppendResult.Failed(CorrelationId.New(),
        [new ConstraintViolation("event-type-1", 42UL, ConstraintType.Schema, "SchemaValidation", "PropertyRequired: #/FlowType", new ConstraintViolationDetails())]);

    async Task Because() => _result = await Forward();

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_name_the_constraint_name() => _result.ExceptionMessages.Single().ShouldContain("SchemaValidation");
    [Fact] void should_name_the_property_path() => _result.ExceptionMessages.Single().ShouldContain("#/FlowType");
    [Fact] void should_name_the_event_type() => _result.ExceptionMessages.Single().ShouldContain("event-type-1");
    [Fact] void should_not_advance() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}
