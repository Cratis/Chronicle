// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_without_provisioning;

public class and_the_sink_is_initialized_later : given.a_sink_factory
{
    ISink _result;

    async Task Because()
    {
        await _sinks.GetWithoutProvisioning(_model);
        _result = await _sinks.GetFor(_model);
    }

    [Fact] void should_return_the_same_sink() => _result.ShouldEqual(_sink);
    [Fact] void should_ensure_indexes_once() => _sink.Received(1).EnsureIndexes();
    [Fact] void should_create_the_sink_once() => _factory.Received(1).CreateFor(string.Empty, string.Empty, _model);
}
