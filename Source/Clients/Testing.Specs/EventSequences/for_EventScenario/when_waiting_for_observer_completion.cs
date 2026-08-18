// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The scenario runs the kernel event sequence grain without a silo, so no projection, reducer or reactor ever runs
/// from an append here. Waiting for their completion must fail loudly rather than report success - a success would
/// let a spec assert that downstream work finished when none of it was ever started.
/// </summary>
public class when_waiting_for_observer_completion : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _appendResult;
    Exception _exception;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _appendResult = await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("hello"));
        _exception = await Catch.Exception(async () => await _appendResult.WaitForCompletion());
    }

    [Fact] void should_have_appended_the_event() => _appendResult.ShouldBeSuccessful();
    [Fact] void should_not_report_that_observers_completed() => _exception.ShouldBeOfExactType<CannotWaitForObserverCompletion>();

    public void Dispose() => _scenario.Dispose();
}
