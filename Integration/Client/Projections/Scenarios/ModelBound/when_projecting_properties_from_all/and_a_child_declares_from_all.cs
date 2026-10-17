// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_properties_from_all.and_a_child_declares_from_all.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_properties_from_all;

[Collection(ChronicleCollection.Name)]
public class and_a_child_declares_from_all(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public static readonly Guid RootId = Guid.Parse("95b0c305-5583-4a6c-ae09-488b932f6005");
        public static readonly Guid ChildId = Guid.Parse("aa2cc88f-a86e-4460-b66f-2b3f835f8554");
        public ChildFromAllRoot Result;

        public override IEnumerable<Type> EventTypes => [typeof(ChildFromAllStarted), typeof(ChildFromAllAdded)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(ChildFromAllRoot)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<ChildFromAllRoot>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(RootId, new ChildFromAllStarted("root-value"));
            var result = await EventStore.EventLog.Append(RootId, new ChildFromAllAdded(ChildId, "child-value"));
            await handler.WaitTillReachesEventSequenceNumber(result.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<ChildFromAllRoot>(RootId.ToString());
        }
    }

    [Fact] void should_map_the_child_value_on_its_creating_event() => Context.Result.Children.Single().LastValue.ShouldEqual("child-value");
    [Fact] void should_keep_the_child_identifier() => Context.Result.Children.Single().Id.ShouldEqual(context.ChildId);
    [Fact] void should_map_the_root_source() => Context.Result.Source.ShouldEqual(context.RootId.ToString());
    [Fact] void should_not_write_the_child_value_to_the_root() => Context.Result.LastValue.ShouldBeNull();
}

[EventType]
public record ChildFromAllStarted(string Value);

[EventType]
public record ChildFromAllAdded(Guid ChildId, string Value);

[FromEvent<ChildFromAllStarted>]
public record ChildFromAllRoot(
    Guid Id,
    [property: FromAll(contextProperty: nameof(EventContext.EventSourceId))] string Source,
    [NotProjected] string? LastValue,
    [ChildrenFrom<ChildFromAllAdded>(key: nameof(ChildFromAllAdded.ChildId))] IEnumerable<ChildFromAllItem> Children);

public record ChildFromAllItem(
    [Key] Guid Id,
    [property: FromAll(property: nameof(ChildFromAllAdded.Value))] string LastValue);

#pragma warning restore SA1402 // File may only contain a single type
