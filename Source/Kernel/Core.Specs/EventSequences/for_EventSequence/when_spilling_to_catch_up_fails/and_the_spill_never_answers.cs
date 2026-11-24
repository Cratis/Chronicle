// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_spilling_to_catch_up_fails;

/// <summary>
/// When the live hand-over fails the sequence spills the queues to catch-up, and that call can be addressed to the dead
/// silo as well. Waiting for it would hold the append response for a second response timeout, so the append is
/// acknowledged while the spill is still outstanding.
/// </summary>
public class and_the_spill_never_answers : given.a_failed_spill
{
    TaskCompletionSource _spill = null!;
    Task _append = null!;
    bool _acknowledged;

    void Establish()
    {
        _spill = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _appendedEventsQueues.SpillToCatchup().Returns(_ => _spill.Task);
    }

    async Task Because()
    {
        _append = AppendEvent();
        _acknowledged = await Acknowledged(_append);
    }

    static async Task<bool> Acknowledged(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(3), TimeProvider.System);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    void Destroy() => _spill.TrySetResult();

    [Fact] void should_acknowledge_the_append_while_the_spill_is_outstanding() => _acknowledged.ShouldBeTrue();
    [Fact] void should_have_asked_for_the_spill() => _appendedEventsQueues.Received(1).SpillToCatchup();
}
