// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_nothing_names_a_sink : given.sink_configurations
{
    ResolvedSink _result;

    void Because() => _result = _configurations.ResolveFor(ReadModel(WellKnownSinkTypes.MongoDB), graph: false);

    [Fact] void should_keep_the_type() => _result.Type.ShouldEqual(WellKnownSinkTypes.MongoDB);
    [Fact] void should_use_the_implicit_default() => _result.IsImplicitDefault.ShouldBeTrue();
}
