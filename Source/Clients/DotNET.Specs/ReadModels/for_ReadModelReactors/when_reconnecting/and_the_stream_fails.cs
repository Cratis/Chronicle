// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_the_stream_fails : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        _streams[0].OnError(new Exception("stream lost"));
        _reactors.Start();
    }

    [Fact] void should_observe_instances_again() => _streams.Count.ShouldEqual(2);
}
