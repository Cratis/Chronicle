// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_releasing_after_a_failed_creation;

/// <summary>
/// A registration that was already past its earlier steps when the store was released still reaches the read-model
/// reactors afterwards. It must find them released.
/// </summary>
public class and_a_registration_in_flight_starts_read_model_reactors_afterwards : given.a_store_whose_seeding_fails_after_read_model_reactors_started
{
    Exception _thrown;

    void Because()
    {
        _eventStore.ReleaseAfterFailedCreation();
        _thrown = Catch.Exception(_eventStore.ReadModelReactors.Start);
    }

    [Fact] void should_not_fail() => _thrown.ShouldBeNull();
    [Fact] void should_not_subscribe_to_the_connection() => (_lifecycle.ConnectedCount, _lifecycle.DisconnectedCount).ShouldEqual((_baselineConnected, _baselineDisconnected));
    [Fact] void should_not_open_a_watch_stream() => _serverStream.HasObservers.ShouldBeFalse();
    [Fact] void should_not_ask_the_kernel_to_watch() => _readModelsService.DidNotReceive().Watch(Arg.Any<WatchRequest>(), Arg.Any<CallContext>());
}
