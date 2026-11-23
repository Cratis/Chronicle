// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_state_concurrently;

public class and_two_callbacks_relinquish_together : given.a_live_observer_with_overlap_guarded_definition
{
    Exception? _firstError;
    Exception? _secondError;

    async Task Because()
    {
        await MoveDefinition();
        _guardedDefinition.HoldNextRead = true;
        var first = _observer.SetHandledStats(99);
        await WaitFor(_guardedDefinition.ReadStarted);
        var second = _observer.SetHandledStats(100);
        _guardedDefinition.ReleaseRead.TrySetResult();
        _firstError = await Capture(first);
        _secondError = await Capture(second);
    }

    static async Task<Exception?> Capture(Task task)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact] void should_not_fail_the_first() => _firstError.ShouldBeNull();
    [Fact] void should_not_fail_the_second() => _secondError.ShouldBeNull();
    [Fact] void should_never_read_the_definition_twice_at_once() => _guardedDefinition.MaxReadsInFlight.ShouldEqual(1);
}
