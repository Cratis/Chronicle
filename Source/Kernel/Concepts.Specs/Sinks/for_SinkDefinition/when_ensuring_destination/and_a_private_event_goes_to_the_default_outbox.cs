// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Sinks.for_SinkDefinition.when_ensuring_destination;

public class and_a_private_event_goes_to_the_default_outbox : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new SinkDefinition(
        SinkConfigurationId.None,
        WellKnownSinkTypes.EventSequence,
        new EventSequenceSinkConfiguration(new EventType("OrderShipped", EventTypeGeneration.First), null, false)).EnsureReadModelSupported());

    [Fact] void should_refuse_explicitly() => _error.ShouldBeOfExactType<PrivateEventCannotBePublishedToOutbox>();
}
