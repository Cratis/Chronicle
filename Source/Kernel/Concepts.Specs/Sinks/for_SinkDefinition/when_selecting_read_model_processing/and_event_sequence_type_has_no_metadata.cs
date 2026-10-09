// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks.for_SinkDefinition.when_selecting_read_model_processing;

public class and_event_sequence_type_has_no_metadata : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence).EnsureReadModelSupported());

    [Fact] void should_refuse_instead_of_defaulting_to_a_read_model() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
}
