// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_storage_is_blocked_for_a_bounded_conclusion : and_storage_is_blocked
{
    protected override Task Acknowledge() => _observer.CaughtUp(JobId.New(), 12UL, 12UL);
}
