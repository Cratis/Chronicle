// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ReadModels.given;

public class a_projection_with_events(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    public EventSourceId EventSourceId;
    public SomeEvent FirstEvent;
    public AnotherEvent SecondEvent;

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
    public override IEnumerable<Type> Projections => [typeof(SomeProjection)];

    protected void Establish()
    {
        EventSourceId = "some-source";
        FirstEvent = new SomeEvent(42);
        SecondEvent = new AnotherEvent("test-value");
    }

    protected async Task AppendEvents()
    {
        var handler = EventStore.Projections.GetHandlerFor<SomeProjection>();
        await handler.WaitTillActive();

        await EventStore.EventLog.Append(EventSourceId, FirstEvent);
        await EventStore.EventLog.Append(EventSourceId, SecondEvent);
    }

    /// <summary>
    /// Polls the materialized read model until it holds at least <paramref name="expectedCount"/>
    /// instances, or the default timeout elapses.
    /// </summary>
    /// <param name="expectedCount">The minimum number of instances to wait for.</param>
    /// <returns>The instances once the expected count is reached.</returns>
    /// <remarks>
    /// <see cref="SomeReadModel"/> is materialized, so <c>GetInstances</c> now reads the sink instead of
    /// replaying — a read straight after appending can race the projection engine's catch-up unless
    /// something polls for the result to appear, the same gap <c>GetInstanceById</c> callers close
    /// elsewhere in this suite.
    /// </remarks>
    protected async Task<IEnumerable<SomeReadModel>> WaitTillInstancesAreVisible(int expectedCount)
    {
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        while (true)
        {
            var instances = await EventStore.ReadModels.GetInstances<SomeReadModel>();
            if (instances.Count() >= expectedCount)
            {
                return instances;
            }

            await Task.Delay(50, cts.Token);
        }
    }
}
