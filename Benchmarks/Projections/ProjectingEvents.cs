// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Benchmarks.Projections;

public class ProjectingEvents : ProjectionJob
{
    IEnumerable<AppendedEvent> _eventsToHandle = Enumerable.Empty<AppendedEvent>();

    [Params(10, 100, 1000)]
    public int NumberOfEvents { get; set; } = 100;

    protected override IEnumerable<Type> EventTypes => TestData.EventTypes;

    [Benchmark]
    public async Task InSequence()
    {
        foreach (var @event in _eventsToHandle)
        {
            await ObserverSupervisor.Handle(@event, true);
        }
    }

    protected override void Setup()
    {
        base.Setup();

        _eventsToHandle = TestData.GenerateAppendedEvents(NumberOfEvents);
    }
}
