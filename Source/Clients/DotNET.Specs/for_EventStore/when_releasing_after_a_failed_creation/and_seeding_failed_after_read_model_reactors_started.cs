// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_releasing_after_a_failed_creation;

public class and_seeding_failed_after_read_model_reactors_started : given.a_store_whose_seeding_fails_after_read_model_reactors_started
{
    Exception _thrown;
    int _connectedWhileFailed;
    bool _streamOpenWhileFailed;

    async Task Because()
    {
        await _projections.Discover();
        _thrown = await Catch.Exception(_eventStore.RegisterAll);
        _connectedWhileFailed = _lifecycle.ConnectedCount;
        _streamOpenWhileFailed = _serverStream.HasObservers;

        // The retry removes its own disconnect handler when it ends, and that happens asynchronously, so the
        // release is awaited together with it before the counts are read.
        var retry = _eventStore.PendingBackgroundRegistrationRetry;
        _eventStore.ReleaseAfterFailedCreation();
        if (retry is not null)
        {
            await retry;
        }
    }

    [Fact] void should_fail_on_seeding() => _thrown.ShouldEqual(_failure);
    [Fact] void should_have_started_watching_before_seeding_failed() => (_connectedWhileFailed > _baselineConnected && _streamOpenWhileFailed).ShouldBeTrue();
    [Fact] void should_leave_the_connected_subscriptions_as_they_were() => _lifecycle.ConnectedCount.ShouldEqual(_baselineConnected);
    [Fact] void should_leave_the_disconnected_subscriptions_as_they_were() => _lifecycle.DisconnectedCount.ShouldEqual(_baselineDisconnected);
    [Fact] void should_close_the_watch_stream() => _serverStream.HasObservers.ShouldBeFalse();
}
