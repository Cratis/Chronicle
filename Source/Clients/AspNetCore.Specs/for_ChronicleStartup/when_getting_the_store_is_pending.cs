// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup;

public class when_getting_the_store_is_pending : given.a_registered_web_host
{
    TaskCompletionSource _storeRequested;

    void Establish()
    {
        _app.UseCratisChronicle();
        _storeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_ =>
        {
            _storeRequested.TrySetResult();
            return _storeCompletion.Task;
        });
    }

    async Task Because()
    {
        await _startup.StartAsync(CancellationToken.None);
        _startedSignal = Task.Run(() => _started.CancelAsync());
        await _storeRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _startedSignal.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_return_from_startup_before_getting_the_store() => _storeCompletion.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_connect_until_the_store_is_available() => _connection.DidNotReceive().Connect();
    [Fact] void should_get_the_event_store_once() => _client.Received(1).GetEventStore("the-store");
}
