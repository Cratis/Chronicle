// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_without_provisioning;

public class and_the_sink_is_not_cached : given.a_sink_factory
{
    ISink _result;

    async Task Because() => _result = await _sinks.GetWithoutProvisioning(_model);

    [Fact] void should_return_the_sink() => _result.ShouldEqual(_sink);
    [Fact] void should_not_ensure_indexes() => _sink.DidNotReceive().EnsureIndexes();
}
