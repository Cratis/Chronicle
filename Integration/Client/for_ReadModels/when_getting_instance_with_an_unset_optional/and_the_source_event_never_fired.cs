// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_with_an_unset_optional.and_the_source_event_never_fired.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_with_an_unset_optional;

/// <summary>
/// End-to-end proof for the optional-scalar sentinel fix. An active model-bound projection whose optional
/// <see cref="OrderTimeline.CompletedAt"/> is sourced by an event that never fires materializes (writes to the
/// MongoDB sink) with that field absent, and reading the model back through the release path must yield
/// <see langword="null"/> — not the type-default sentinel (<c>0001-01-01</c>). This drives the whole
/// event -> projection -> sink -> release loop, proving both that the unset optional is stored absent and that
/// it releases as null.
/// </summary>
/// <param name="context">The scenario context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_source_event_never_fired(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid OrderId;
        public OrderTimeline Result;

        public override IEnumerable<Type> EventTypes => [typeof(OrderPlaced), typeof(OrderCompleted)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(OrderTimeline)];

        async Task Because()
        {
            OrderId = Guid.Parse("b1c2d3e4-f5a6-7890-bcde-111111111111");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<OrderTimeline>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            // Only the creating event fires; OrderCompleted — the source of the optional CompletedAt — never does.
            var appendResult = await EventStore.EventLog.Append(OrderId, new OrderPlaced("ORD-1"));
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<OrderTimeline>(OrderId.ToString());
        }
    }

    [Fact] void should_return_the_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_required_timestamp() => Context.Result.PlacedAt.ShouldNotEqual(default);
    [Fact] void should_leave_the_unset_optional_null() => Context.Result.CompletedAt.ShouldBeNull();
}

[EventType]
public record OrderPlaced(string Reference);

[EventType]
public record OrderCompleted;

[FromEvent<OrderPlaced>]
public record OrderTimeline(
    Guid Id,
    string Reference,

    [property: SetFromContext<OrderPlaced>(nameof(EventContext.Occurred))]
    DateTimeOffset PlacedAt,

    [property: SetFromContext<OrderCompleted>(nameof(EventContext.Occurred))]
    DateTimeOffset? CompletedAt);

#pragma warning restore SA1402
