// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_round_tripping;

public class and_metadata_has_no_destination_or_event_sink_type : Specification
{
    SinkDefinition _definition;
    Contracts.Sinks.SinkDefinition _wire;
    SinkDefinition _result;

    void Establish() => _definition = new(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB, new(new EventType("PublicStateChanged", new EventTypeGeneration(7))));

    void Because()
    {
        _wire = _definition.ToContract();
        _result = _wire.ToChronicle();
    }

    [Fact] void should_prevent_old_kernels_from_selecting_mongodb() => _wire.TypeId.ShouldEqual(WellKnownSinkTypes.EventSequence.Value);
    [Fact] void should_default_to_outbox() => _result.EventSequence!.Destination.ShouldEqual(EventSequenceId.Outbox);
    [Fact] void should_never_default_to_event_log() => _result.EventSequence!.Destination.IsEventLog.ShouldBeFalse();
}
