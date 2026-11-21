// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// The background work retries what a busy sibling did not answer, backs off between passes and completes.
/// </summary>
public class and_a_sibling_times_out_in_the_background : given.a_startup_task_with_incomplete_persisted_capture
{
    const int TimesOut = 3;

    IObserver _applicationObserver;
    Exception _error;
    int _calls;

    void Establish()
    {
        _applicationObserver = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey("unsubscribed", _eventStore, _namespace, EventSequenceId.Outbox)).Returns(_applicationObserver);
        _applicationObserver.Ensure().Returns(_ => ++_calls <= TimesOut
            ? Task.FromException(new TimeoutException("Response did not arrive on time"))
            : Task.CompletedTask);
        _observerStateStorage.GetAll().Returns([
            ObserverState.Empty with { Identifier = PatternCapture.ObserverIdentifier },
            ObserverState.Empty with { Identifier = "unsubscribed", RunningState = ObserverRunningState.Disconnected }
        ]);
        _eventStoreStorage.Observers.GetAll().Returns([
            new ObserverDefinition { Identifier = PatternCapture.ObserverIdentifier, EventSequenceId = EventSequenceId.Log },
            new ObserverDefinition { Identifier = "unsubscribed", EventSequenceId = EventSequenceId.Outbox }
        ]);
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_try_until_it_succeeds() => _calls.ShouldEqual(TimesOut + 1);
    [Fact] void should_back_off_between_passes() => new[] { 5, 10, 20 }.All(_ => RequestedDelays.Contains(TimeSpan.FromSeconds(_))).ShouldBeTrue();
    [Fact] void should_report_each_failure_with_its_retry_delay() => LogEntries.Count(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkUnitFailed)).ShouldEqual(TimesOut);
    [Fact] void should_report_the_summary_once() => LogEntries.Count(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkFinished)).ShouldEqual(1);
    [Fact] void should_report_the_event_store_completed_on_the_last_pass() => LogMessages.Any(_ => _.Contains("0 failed, pass 4", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_give_up() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkGaveUp)).ShouldBeFalse();
}
