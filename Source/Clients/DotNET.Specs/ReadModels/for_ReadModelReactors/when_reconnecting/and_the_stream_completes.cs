// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_the_stream_completes : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        _streams[0].OnCompleted();
        _lifecycle.OnConnected += Raise.Event<Connected>();
    }

    [Fact] void should_observe_instances_again() => _streams.Count.ShouldEqual(2);
}
