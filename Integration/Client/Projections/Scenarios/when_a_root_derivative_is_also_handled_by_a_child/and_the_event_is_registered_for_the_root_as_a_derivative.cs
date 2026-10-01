// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_ReadModels;
using Cratis.Serialization;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_a_root_derivative_is_also_handled_by_a_child.and_the_event_is_registered_for_the_root_as_a_derivative.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_a_root_derivative_is_also_handled_by_a_child;

/// <summary>
/// An event the root handles through a base event type, and a child collection also handles, still creates the root
/// read model's instance: the stored root is initialized and carries the projection's initial values. The fluent
/// builder does not send a derivative registration, so the projection definition is registered directly.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_event_is_registered_for_the_root_as_a_derivative(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(WidgetRegistered), typeof(WidgetRegisteredAtDepot)];

        async Task Because()
        {
            await EventStore.ReadModels.Register<DepotWidget>();

            var namingPolicy = Services.GetService<INamingPolicy>() ?? new DefaultNamingPolicy();

            var registeredEventType = ToContract(EventStore.EventTypes.GetEventTypeFor(typeof(WidgetRegistered)));
            var registeredAtDepotEventType = ToContract(EventStore.EventTypes.GetEventTypeFor(typeof(WidgetRegisteredAtDepot)));
            var definition = new Contracts.Projections.ProjectionDefinition
            {
                EventSequenceId = "event-log",
                Identifier = typeof(DepotWidget).FullName!,
                ReadModel = typeof(DepotWidget).FullName!,
                IsActive = true,
                InitialModelState = $$"""{"{{nameof(DepotWidget.Status)}}":"pending"}""",
                FromEvery =
                [
                    new()
                    {
                        EventTypes = [registeredEventType, registeredAtDepotEventType],
                        From = new() { Key = WellKnownExpressions.EventSourceId, Properties = new Dictionary<string, string> { [nameof(DepotWidget.Name)] = namingPolicy.GetPropertyName(nameof(WidgetRegistered.Name)) } }
                    }
                ],
                Children = new Dictionary<string, Contracts.Projections.ChildrenDefinition>
                {
                    [nameof(DepotWidget.Parts)] = new()
                    {
                        IdentifiedBy = nameof(DepotPart.PartId),
                        From = new Dictionary<Contracts.Events.EventType, Contracts.Projections.FromDefinition>
                        {
                            [registeredAtDepotEventType] = new()
                            {
                                Key = namingPolicy.GetPropertyName(nameof(WidgetRegisteredAtDepot.PartId)),
                                Properties = new Dictionary<string, string> { [nameof(DepotPart.Label)] = namingPolicy.GetPropertyName(nameof(WidgetRegisteredAtDepot.Name)) }
                            }
                        }
                    }
                }
            };

            var servicesAccessor = (IChronicleServicesAccessor)EventStore.Connection;
            await servicesAccessor.Services.Projections.Register(new()
            {
                EventStore = EventStore.Name,
                Owner = Contracts.Projections.ProjectionOwner.Client,
                Projections = [definition],
                FullSet = false
            });

            await EventStore.EventLog.Append("depot-widget-1", new WidgetRegisteredAtDepot("Sprocket", "part-1"));
            StoredDocument = await StoredReadModelDocument.ReadWhen(
                ChronicleFixture,
                nameof(DepotWidget),
                document => StoredReadModelDocument.Field(document, "parts") is BsonArray { Count: 1 });
        }

        static Contracts.Events.EventType ToContract(EventType eventType) => new()
        {
            Id = eventType.Id,
            Generation = eventType.Generation.Value,
            Tombstone = eventType.Tombstone
        };
    }

    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() =>
        (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();

    [Fact] void should_store_the_root_property() =>
        (!Context.DocumentCanBeInspected || string.Equals(StoredReadModelDocument.Field(Context.StoredDocument, "name")?.ToString(), "Sprocket", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] void should_store_the_child() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.StoredDocument, "parts") is BsonArray { Count: 1 }).ShouldBeTrue();

    [Fact] void should_store_the_root_as_initialized() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.StoredDocument, "__initialized") is BsonBoolean { Value: true }).ShouldBeTrue();

    [Fact] void should_store_the_initial_values() =>
        (!Context.DocumentCanBeInspected || string.Equals(StoredReadModelDocument.Field(Context.StoredDocument, "status")?.ToString(), "pending", StringComparison.Ordinal)).ShouldBeTrue();
}

[EventType]
public record WidgetRegistered(string Name);

[EventType]
public record WidgetRegisteredAtDepot(string Name, string PartId) : WidgetRegistered(Name);

public record DepotPart(string PartId, string Label);

public record DepotWidget(string Id, string Name, string Status, IEnumerable<DepotPart> Parts);

#pragma warning restore SA1402
