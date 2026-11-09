// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_a_graph_names_a_read_model_sink : given.sink_configurations
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.Neo4j, "primary"), graph: true));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<SinkConfigurationIsForAnotherKindOfReadModel>();
}
