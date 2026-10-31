// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class and_the_stream_fails : given.a_collection_stream
{
    readonly Exception _failure = new("Stream failed");

    async Task Because()
    {
        _observation = _readModels.Observe<Row>();
        Subscribe();
        _stream.OnError(_failure);
        (await _signal.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
    }

    [Fact] void should_propagate_the_error() => _error.ShouldEqual(_failure);
    [Fact] void should_close_the_stream() => _disposed.ShouldBeTrue();
    [Fact] void should_not_resubscribe() => _service.Received(1).ObserveInstances(Arg.Any<Contracts.ReadModels.ObserveInstancesRequest>());
}
