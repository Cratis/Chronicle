// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class TriggeredRepairDeadline : TimeProvider
{
    TimerCallback? _callback;
    object? _state;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        _callback = callback;
        _state = state;
        return new DeadlineTimer();
    }

    public void Expire() => _callback?.Invoke(_state);

    sealed class DeadlineTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
