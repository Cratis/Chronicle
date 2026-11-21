// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

public class when_the_floor_is_no_longer_reached : given.a_queue_with_a_stored_durable_subscription
{
    async Task Because()
    {
        _clusterReleases.Summarize().Returns(new ClusterReleaseSummary([(new SemanticVersion(19, 172, 0), 1), (new SemanticVersion(19, 173, 0), 1)], 0, 0));
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_not_wake_anything() => _calls.ShouldBeEmpty();
}
