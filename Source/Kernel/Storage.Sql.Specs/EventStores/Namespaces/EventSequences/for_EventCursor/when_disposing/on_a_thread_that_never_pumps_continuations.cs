// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventCursor.when_disposing;

public class on_a_thread_that_never_pumps_continuations : given.a_cursor_over_a_context_whose_async_disposal_suspends
{
    static readonly TimeSpan _maxTimeToDispose = TimeSpan.FromSeconds(5);

    bool _completed;

    void Because()
    {
        // An Orleans activation thread never pumps posted continuations while it is blocked, so disposal that
        // waits for one never returns. Standing in a synchronization context that drops everything posted to it
        // reproduces that without a silo.
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new discarding_synchronization_context());
            _cursor.Dispose();
        })
        {
            IsBackground = true
        };

        thread.Start();
        _completed = thread.Join(_maxTimeToDispose);
    }

    [Fact] void should_complete_without_waiting_for_a_continuation() => _completed.ShouldBeTrue();

    sealed class discarding_synchronization_context : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }
    }
}
