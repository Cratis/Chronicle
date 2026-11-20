// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_deactivating;

public class and_the_state_write_never_completes : given.an_event_sequence_with_a_hanging_state_write
{
    protected override int StatePersistenceInterval => 1000;

    bool _completed;

    async Task Establish()
    {
        await AppendAnEvent();
        await AppendAnEvent();
        ((given.EventSequenceWhoseStateWriteNeverCompletes)_eventSequence).Hang = true;
    }

    async Task Because()
    {
        var deactivation = _eventSequence.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, string.Empty), CancellationToken.None);
        await ((given.EventSequenceWhoseStateWriteNeverCompletes)_eventSequence).Hanging.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _deadline.CancelAsync();
        try
        {
            await deactivation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _completed = true;
        }
        catch (TimeoutException)
        {
        }
    }

    [Fact] void should_complete_deactivation_at_the_deadline() => _completed.ShouldBeTrue();
}
