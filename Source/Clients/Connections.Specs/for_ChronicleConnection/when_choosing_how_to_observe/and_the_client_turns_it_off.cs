// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_choosing_how_to_observe;

public class and_the_client_turns_it_off : given.services_with_a_stream_per_observer
{
    Services _result;

    void Because() => _result = Choose(false, KernelCapabilities.MultiplexedObservation);

    [Fact] void should_keep_a_stream_per_reactor() => _result.Reactors.ShouldEqual(_perObserver.Reactors);
    [Fact] void should_keep_a_stream_per_reducer() => _result.Reducers.ShouldEqual(_perObserver.Reducers);
    [Fact] void should_not_reach_for_the_multiplexed_observation() => _multiplexedRequested.ShouldEqual(0);
}
