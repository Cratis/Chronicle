// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.XUnit.Integration;

namespace Cratis.Chronicle.Integration.for_ReadModels.given;

public class a_projection_with_many_instances(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    public const int TotalInstances = 5;

    public EventSourceId[] EventSourceIds;
    public SomeEvent[] SomeEvents;
    public AnotherEvent[] AnotherEvents;

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
    public override IEnumerable<Type> Projections => [typeof(SomeProjection)];

    protected void Establish()
    {
        EventSourceIds =
        [
            "source-1",
            "source-2",
            "source-3",
            "source-4",
            "source-5"
        ];

        SomeEvents =
        [
            new SomeEvent(10),
            new SomeEvent(20),
            new SomeEvent(30),
            new SomeEvent(40),
            new SomeEvent(50)
        ];

        AnotherEvents =
        [
            new AnotherEvent("value-1"),
            new AnotherEvent("value-2"),
            new AnotherEvent("value-3"),
            new AnotherEvent("value-4"),
            new AnotherEvent("value-5")
        ];
    }

    protected async Task AppendAllEvents()
    {
        var projection = EventStore.Projections.GetHandlerFor<SomeProjection>();
        await projection.WaitTillActive();

        for (var i = 0; i < TotalInstances; i++)
        {
            await EventStore.EventLog.Append(EventSourceIds[i], SomeEvents[i]);
            await EventStore.EventLog.Append(EventSourceIds[i], AnotherEvents[i]);
        }

        // Poll the sink-based paginated endpoint (not in-memory GetAllInstances) to ensure
        // all instances are materialised in the actual storage sink before assertions run.
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        var readModels = Services.GetRequiredService<IServices>().ReadModels;
        while (true)
        {
            var response = await readModels.GetInstances(new GetInstancesRequest
            {
                EventStore = Constants.EventStore,
                Namespace = "Default",
                ReadModel = typeof(SomeReadModel).FullName,
                Page = 0,
                PageSize = TotalInstances + 1
            });

            if (response.TotalCount >= TotalInstances)
            {
                break;
            }

            await Task.Delay(100, cts.Token);
        }
    }
}
