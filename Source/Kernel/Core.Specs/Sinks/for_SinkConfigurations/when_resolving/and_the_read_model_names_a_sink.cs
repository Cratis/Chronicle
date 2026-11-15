// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_the_read_model_names_a_sink : given.sink_configurations
{
    ResolvedSink _result;

    void Because() => _result = _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.MongoDB, "reporting"), graph: false);

    [Fact] void should_use_the_type_of_the_configuration() => _result.Type.ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_use_the_configured_name() => _result.Name.ShouldEqual(new SinkName("Reporting"));
}
