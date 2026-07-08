// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_join_and_no_auto_map.and_a_joined_event_carries_a_no_auto_map_property.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_join_and_no_auto_map;

[Collection(ChronicleCollection.Name)]
public class and_a_joined_event_carries_a_no_auto_map_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid OrderId;
        public Guid CustomerId;
        public OrderJoinSummary Result;

        public override IEnumerable<Type> EventTypes => [typeof(OrderPlacedForCustomer), typeof(PartnerRegisteredForOrder)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(OrderJoinSummary)];

        async Task Because()
        {
            OrderId = Guid.Parse("f1d2b3a4-1111-4a2b-9c3d-0a1b2c3d4e5f");
            CustomerId = Guid.Parse("a9b8c7d6-2222-4b3c-8d4e-5f6a7b8c9d0e");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<OrderJoinSummary>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(CustomerId.ToString(), new PartnerRegisteredForOrder("Ada", "Gold"));
            var appendResult = await EventStore.EventLog.Append(OrderId.ToString(), new OrderPlacedForCustomer(CustomerId, "Open"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<OrderJoinSummary>(OrderId.ToString());
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_the_explicitly_sourced_status() => Context.Result.Status.ShouldEqual("Open");
    [Fact] void should_join_the_partner_name() => Context.Result.PartnerName.ShouldEqual("Ada");
}
