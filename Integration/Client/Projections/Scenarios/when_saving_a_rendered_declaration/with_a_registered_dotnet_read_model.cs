// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_saving_a_rendered_declaration.with_a_registered_dotnet_read_model.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_saving_a_rendered_declaration;

[Collection(ChronicleCollection.Name)]
public class with_a_registered_dotnet_read_model(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_projection_and_events_appended_to_it<OrderProjection, Order>(fixture)
    {
        public string Declaration;
        public SaveProjectionResult SaveResult;
        public Order Original;
        public Order AfterSave;
        public DateTimeOffset OccurredAfterSave;

        public override IEnumerable<Type> EventTypes => [typeof(OrderPlaced)];

        void Establish() => EventsToAppend.Add(new OrderPlaced("Before save"));

        async Task Because()
        {
            Original = Result;
            var projections = ((IChronicleServicesAccessor)EventStore.Connection).Services.Projections;
            var declarations = await projections.GetAllDeclarations(new() { EventStore = EventStore.Name });
            Declaration = declarations.Single(_ => _.Identifier == EventStore.Projections.GetProjectionIdFor<OrderProjection>().Value).Declaration;
            SaveResult = await projections.Save(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                Declaration = Declaration
            });
            SaveResult.Errors.ShouldBeEmpty();
            await Projection.WaitTillSubscribed();
            EventSourceId = Guid.NewGuid().ToString();
            ReadModelId = EventSourceId.Value;
            var appended = await EventStore.EventLog.Append(EventSourceId, new OrderPlaced("After save"));
            await WaitForProjectionAndSetResult(appended.SequenceNumber, requireMaterialized: true);
            AfterSave = Result;
            var events = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(EventSourceId, [typeof(OrderPlaced).GetEventType()]);
            OccurredAfterSave = events[^1].Context.Occurred;
        }
    }

    [Fact] void should_render_the_full_read_model_identifier() => Context.Declaration.ShouldContain($"=> {typeof(Order).FullName}");
    [Fact] void should_save_the_declaration_unchanged() => Context.SaveResult.Errors.ShouldBeEmpty();
    [Fact] void should_map_the_event_property_after_save() => Context.AfterSave.Name.ShouldEqual("After save");
    [Fact] void should_keep_the_boolean_type_and_value() => Context.AfterSave.IsActive.ShouldEqual(Context.Original.IsActive);
    [Fact] void should_keep_the_context_mapping() => Context.AfterSave.Occurred.ShouldEqual(Context.OccurredAfterSave);
}
