// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_a_late_error_arrives_from_the_previous_stream : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        Reconnect();
        _observers[0].OnError(new Exception("late stream failure"));
    }

    [Fact] void should_leave_the_current_stream_subscribed() => _streams[1].HasObservers.ShouldBeTrue();
}
