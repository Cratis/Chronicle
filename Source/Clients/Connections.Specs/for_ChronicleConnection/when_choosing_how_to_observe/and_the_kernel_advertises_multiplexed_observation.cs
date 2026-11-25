// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.Multiplexed;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_choosing_how_to_observe;

public class and_the_kernel_advertises_multiplexed_observation : given.services_with_a_stream_per_observer
{
    Services _result;

    void Because() => _result = Choose(true, KernelCapabilities.MultiplexedObservation);

    [Fact] void should_observe_reactors_over_the_shared_stream() => _result.Reactors.ShouldBeOfExactType<MultiplexedReactors>();
    [Fact] void should_observe_reducers_over_the_shared_stream() => _result.Reducers.ShouldBeOfExactType<MultiplexedReducers>();
    [Fact] void should_keep_everything_else() => _result.Compliance.ShouldEqual(_perObserver.Compliance);
}
