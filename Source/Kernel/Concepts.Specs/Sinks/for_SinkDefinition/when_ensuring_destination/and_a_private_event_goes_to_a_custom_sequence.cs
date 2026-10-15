// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Sinks.for_SinkDefinition.when_ensuring_destination;

public class and_a_private_event_goes_to_a_custom_sequence : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new SinkDefinition(
        SinkConfigurationId.None,
        WellKnownSinkTypes.EventSequence,
        new EventSequenceSinkConfiguration(new EventType("OrderShipped", EventTypeGeneration.First), new EventSequenceId("partner-feed"), false)).EnsureReadModelSupported());

    [Fact] void should_accept() => _error.ShouldBeNull();
}
