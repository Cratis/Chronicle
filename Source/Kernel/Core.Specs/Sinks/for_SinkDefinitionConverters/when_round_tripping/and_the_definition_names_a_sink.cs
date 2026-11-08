// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_round_tripping;

public class and_the_definition_names_a_sink : Specification
{
    SinkDefinition _definition;
    Contracts.Sinks.SinkDefinition _wire;
    SinkDefinition _result;

    void Establish() => _definition = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL) { Name = "reporting" };

    void Because()
    {
        _wire = _definition.ToContract();
        _result = _wire.ToChronicle();
    }

    [Fact] void should_carry_the_name_on_the_wire() => _wire.Name.ShouldEqual("reporting");
    [Fact] void should_round_trip_the_definition() => _result.ShouldEqual(_definition);
}
