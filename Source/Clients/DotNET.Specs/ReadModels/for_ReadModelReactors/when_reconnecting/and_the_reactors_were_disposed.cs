// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_the_reactors_were_disposed : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        _reactors.Dispose();
        _lifecycle.OnConnected += Raise.Event<Connected>();
    }

    [Fact] void should_not_observe_again() => _streams.Count.ShouldEqual(1);
    [Fact] void should_dispose_the_current_stream() => _streams[0].HasObservers.ShouldBeFalse();
}
