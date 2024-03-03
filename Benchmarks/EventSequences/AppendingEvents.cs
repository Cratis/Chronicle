// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identities;
using Cratis.Kernel.Grains.EventSequences;

namespace Benchmarks.EventSequences;

public class AppendingEvents : EventLogJob
{
    IEnumerable<EventToAppend> _eventsToAppend = Enumerable.Empty<EventToAppend>();

    [Params(10, 100, 1000)]
    public int NumberOfEvents { get; set; } = 1000;

    protected override IEnumerable<Type> EventTypes => TestData.EventTypes;

    [Benchmark]
    [InvocationCount(1)]
    public Task InSequence() => Perform(async eventSequence =>
    {
        foreach (var @event in _eventsToAppend)
        {
            await eventSequence.Append(
                @event.EventSourceId,
                @event.EventType,
                @event.Content,
                GlobalVariables.BenchmarkCausation,
                Identity.System);
        }
    });

    [Benchmark]
    [InvocationCount(1)]
    public Task WithAppendMany() => Perform(async eventSequence => await eventSequence.AppendMany(_eventsToAppend, GlobalVariables.BenchmarkCausation, Identity.System));

    protected override void Setup()
    {
        base.Setup();

        _eventsToAppend = TestData.GenerateEventsToAppend(NumberOfEvents);
    }
}
