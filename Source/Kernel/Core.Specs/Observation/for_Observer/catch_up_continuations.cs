// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

static class catch_up_continuations
{
    public static Task RunCatchUpContinuation(this TestKitSilo silo)
    {
        var registrations = silo.TimerRegistry.Mock.Invocations
            .Where(_ => _.Method.Name == "RegisterGrainTimer")
            .ToArray();
        var index = Array.FindLastIndex(registrations, _ => _.Arguments.OfType<GrainTimerCreationOptions>()
            .Any(options => options.DueTime == TimeSpan.Zero && options.Period == Timeout.InfiniteTimeSpan && !options.Interleave));
        index.ShouldBeGreaterThanOrEqual(0);
        return silo.TimerRegistry.FireAsync(index);
    }
}
