// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_running_many_partitions;

public class and_the_window_is_asked_for : given.a_replay_observer_job
{
    int? _window;

    void Because() => _window = _job.MaxActiveStepsForTesting;

    [Fact] void should_bound_the_active_steps_to_what_the_observer_can_serve() => _window.ShouldEqual(ReplayObserver.ActiveStepWindow);
}
