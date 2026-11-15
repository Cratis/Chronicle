// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_the_read_model_names_an_unknown_sink : given.sink_configurations
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.MongoDB, "archive"), graph: false));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<UnknownSinkName>();
    [Fact] void should_name_the_read_model() => _error.Message.ShouldContain("'orders'");
    [Fact] void should_name_the_sink() => _error.Message.ShouldContain("'archive'");
}
