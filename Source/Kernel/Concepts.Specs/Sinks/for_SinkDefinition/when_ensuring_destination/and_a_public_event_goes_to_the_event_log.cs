// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Sinks.for_SinkDefinition.when_ensuring_destination;

public class and_a_public_event_goes_to_the_event_log : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new SinkDefinition(
        SinkConfigurationId.None,
        WellKnownSinkTypes.EventSequence,
        new EventSequenceSinkConfiguration(new EventType("OrderShipped", EventTypeGeneration.First), EventSequenceId.Log, true)).EnsureReadModelSupported());

    [Fact] void should_refuse_explicitly() => _error.ShouldBeOfExactType<EventLogIsNotAPublicationTarget>();
}
