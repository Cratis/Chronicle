// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_connected_without_a_drop : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        _lifecycle.OnConnected += Raise.Event<Connected>();
        _reactors.Start();
    }

    [Fact] void should_keep_the_current_stream() => _streams.Count.ShouldEqual(1);
}
