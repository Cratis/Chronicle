// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_a_late_window_arrives_from_the_previous_stream : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        Reconnect();
        _observers[0].OnNext([new Item("obsolete", "late")]);
        _streams[1].OnNext(_items);
    }

    [Fact] void should_ignore_the_obsolete_window() => _changes.ShouldBeEmpty();
}
