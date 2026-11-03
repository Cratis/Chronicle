// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_after_the_layout_is_adopted;

public class and_the_adoption_succeeded : given_a_sink_that_needs_its_layout
{
    TaskCompletionSource _readReachedTheTable = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _adopted;
    bool _readPassedWhileTheLayoutLockWasHeld;

    protected override Task OnTableOpening(int ordinal)
    {
        if (Volatile.Read(ref _adopted))
        {
            _readReachedTheTable.TrySetResult();
        }

        return Task.CompletedTask;
    }

    async Task Because()
    {
        await AdoptLayout();
        Volatile.Write(ref _adopted, true);
        _readPassedWhileTheLayoutLockWasHeld = ReadWhileHoldingTheLayoutLock(out var read);
        await read;
    }

    bool ReadWhileHoldingTheLayoutLock(out Task read)
    {
        var layoutLock = typeof(Sink).GetField("_layoutLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_sink)!;
        lock (layoutLock)
        {
            // A read that took the layout lock cannot reach the table until this thread lets go of it.
            read = Task.Run(() => _sink.GetInstances());
            return _readReachedTheTable.Task.Wait(TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_not_take_the_layout_lock_to_read() => _readPassedWhileTheLayoutLockWasHeld.ShouldBeTrue();
}
