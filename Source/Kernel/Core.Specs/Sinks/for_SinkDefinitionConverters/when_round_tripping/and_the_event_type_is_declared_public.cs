// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_round_tripping;

public class and_the_event_type_is_declared_public : Specification
{
    SinkDefinition _definition;
    SinkDefinition _result;

    void Establish() => _definition = new(
        SinkConfigurationId.None,
        WellKnownSinkTypes.EventSequence,
        new(new EventType("OrderShipped", EventTypeGeneration.First), EventSequenceId.Outbox, true));

    void Because() => _result = _definition.ToContract().ToChronicle();

    [Fact] void should_carry_the_declaration_to_the_kernel() => _result.EventSequence!.IsPublic.ShouldBeTrue();
}
