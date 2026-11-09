// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_the_connection_drops_and_returns : given.materialized_reactors_on_a_lifecycle
{
    void Because() => Reconnect();

    [Fact] void should_observe_instances_again() => _streams.Count.ShouldEqual(2);
    [Fact] void should_dispose_the_previous_stream() => _streams[0].HasObservers.ShouldBeFalse();
}
