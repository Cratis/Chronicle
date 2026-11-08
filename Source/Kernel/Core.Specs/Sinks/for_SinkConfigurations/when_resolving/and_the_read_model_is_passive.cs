// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.when_resolving;

public class and_the_read_model_is_passive : given.sink_configurations
{
    ResolvedSink _result;

    protected override SinksOptions Sinks => new() { ReadModels = "primary", Configurations = base.Sinks.Configurations };

    void Because() => _result = _configurations.ResolveFor(ReadModel(SinkTypeId.None), graph: false);

    [Fact] void should_stay_without_a_sink() => _result.Type.ShouldEqual(SinkTypeId.None);
    [Fact] void should_not_be_given_the_configured_default() => _result.IsImplicitDefault.ShouldBeTrue();
}
