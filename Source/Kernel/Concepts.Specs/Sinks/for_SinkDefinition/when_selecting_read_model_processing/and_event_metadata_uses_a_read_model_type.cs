// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Sinks.for_SinkDefinition.when_selecting_read_model_processing;

public class and_event_metadata_uses_a_read_model_type : Specification
{
    SinkDefinition _definition;
    Exception _error;

    void Establish() => _definition = new(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB, new(new EventType("PublicStateChanged", new EventTypeGeneration(7))));
    void Because() => _error = Catch.Exception(_definition.EnsureReadModelSupported);

    [Fact] void should_refuse_instead_of_discarding_metadata() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
}
