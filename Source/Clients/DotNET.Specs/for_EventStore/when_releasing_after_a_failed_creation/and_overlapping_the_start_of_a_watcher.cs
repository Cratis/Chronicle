// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_releasing_after_a_failed_creation;

/// <summary>
/// The release runs while a watcher is inside the creation of its server stream, which is the one point where neither
/// the watcher nor the manager can reach the stream yet. The stream must not survive the release.
/// </summary>
public class and_overlapping_the_start_of_a_watcher : given.a_store_whose_seeding_fails_after_read_model_reactors_started
{
    readonly ManualResetEventSlim _creating = new();
    readonly ManualResetEventSlim _released = new();
    Task _starting;

    void Establish() =>
        _readModelsService.Watch(Arg.Any<WatchRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            _creating.Set();
            _released.Wait(TimeSpan.FromSeconds(30));
            return _serverStream;
        });

    async Task Because()
    {
        _starting = Task.Run(_eventStore.ReadModelReactors.Start);
        _creating.Wait(TimeSpan.FromSeconds(30));
        _eventStore.ReleaseAfterFailedCreation();
        _released.Set();
        await Catch.Exception(() => _starting);
    }

    [Fact] void should_not_leave_the_watch_stream_open() => _serverStream.HasObservers.ShouldBeFalse();
    [Fact] void should_not_leave_a_connection_subscription() => (_lifecycle.ConnectedCount, _lifecycle.DisconnectedCount).ShouldEqual((_baselineConnected, _baselineDisconnected));
}
