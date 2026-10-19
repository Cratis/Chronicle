// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

/// <summary>
/// A step whose report timed out cannot tell whether the observer took it, so it sends the same report again. The
/// observer takes it once, and still takes the step's next report and another step's report for the same events.
/// </summary>
public class and_a_step_sends_a_report_again : given.an_observer_coalescing_writes
{
    const string TypeA = "a";

    Key _partition;
    JobStepId _step;
    JobStepId _otherStep;

    void Establish()
    {
        _partition = new Key("partition", ArrayIndexers.NoIndexers);
        _step = JobStepId.New();
        _otherStep = JobStepId.New();
    }

    async Task Because()
    {
        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 3UL }, _step, 10UL);
        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 3UL }, _step, 10UL);
        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 2UL }, _step, 12UL);
        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 2UL }, _step, 12UL);
        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [TypeA] = 1UL }, _otherStep, 10UL);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_take_each_report_exactly_once() => _stateStorage.State.HandledEventCount.Value.ShouldEqual(6UL);
    [Fact] void should_take_each_report_exactly_once_per_event_type() => _stateStorage.State.HandledEventCountPerEventType[TypeA].Value.ShouldEqual(6UL);
}
