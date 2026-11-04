// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_the_snapshot_is_replayed : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        Reconnect();
        _streams[1].OnNext(_items);
    }

    [Fact] void should_not_dispatch_duplicates() => _changes.ShouldBeEmpty();
}
