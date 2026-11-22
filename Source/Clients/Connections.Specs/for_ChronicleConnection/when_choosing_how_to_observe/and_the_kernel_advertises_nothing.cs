// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_choosing_how_to_observe;

public class and_the_kernel_advertises_nothing : given.services_with_a_stream_per_observer
{
    Services _result;

    void Because() => _result = Choose(true);

    [Fact] void should_keep_a_stream_per_reactor() => _result.Reactors.ShouldEqual(_perObserver.Reactors);
    [Fact] void should_keep_a_stream_per_reducer() => _result.Reducers.ShouldEqual(_perObserver.Reducers);
}
