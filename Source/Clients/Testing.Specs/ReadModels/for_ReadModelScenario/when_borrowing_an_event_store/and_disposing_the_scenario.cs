// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_disposing_the_scenario : a_borrowed_store
{
    ReadModelScenario<Tally> _disposed;
    ReadModelScenario<Tally> _next;
    AppendResult _append;
    Exception _observeError;

    void Establish() => _disposed = Borrowed<Tally>();

    async Task Because()
    {
        await _disposed.Given.ForEventSource(EventSourceId.New()).Events(new Tallied());
        _ = _disposed.Instance;
        _disposed.Dispose();

        _append = await _store.EventLog.Append(EventSourceId.New(), new Tallied());
        _next = Borrowed<Tally>();
        await _next.Given.ForEventSource(EventSourceId.New()).Events(new Tallied(), new Tallied());
        _observeError = Catch.Exception(() => _store.ReadModels.Observe<Tally>().Subscribe(_ => { }).Dispose());
    }

    [Fact] void should_leave_the_store_appending() => _append.IsSuccess.ShouldBeTrue();
    [Fact] void should_leave_the_store_folding_new_scenarios() => _next.Instance!.Count.ShouldEqual(2);
    [Fact] void should_leave_the_stores_own_read_models_observable() => _observeError.ShouldBeNull();
}
