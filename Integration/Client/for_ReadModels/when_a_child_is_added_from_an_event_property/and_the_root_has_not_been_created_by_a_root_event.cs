// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_child_is_added_from_an_event_property.and_the_root_has_not_been_created_by_a_root_event.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_child_is_added_from_an_event_property;

/// <summary>
/// An event a child collection handles only as the value of an event property does not create its root read model's
/// instance: the root is stored as a placeholder, not initialized and without the projection's initial values, until
/// a root event arrives and fills them in. The fluent builder does not emit a children event-property registration, so
/// the projection definition is registered directly.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_root_has_not_been_created_by_a_root_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId ShelfId { get; } = "child-only-shelf-1";
        public BsonDocument? DocumentAfterTheChildOnlyEvent { get; private set; }
        public BsonDocument? DocumentAfterTheRootEvent { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(ChildOnlyShelfCreated), typeof(ChildOnlyBookShelved)];

        async Task Because()
        {
            await EventStore.ReadModels.Register<ChildOnlyShelf>();

            var namingPolicy = Services.GetService<INamingPolicy>() ?? new DefaultNamingPolicy();

            var createdEventType = EventStore.EventTypes.GetEventTypeFor(typeof(ChildOnlyShelfCreated));
            var shelvedEventType = EventStore.EventTypes.GetEventTypeFor(typeof(ChildOnlyBookShelved));
            var definition = new Contracts.Projections.ProjectionDefinition
            {
                EventSequenceId = "event-log",
                Identifier = typeof(ChildOnlyShelf).FullName!,
                ReadModel = typeof(ChildOnlyShelf).FullName!,
                IsActive = true,
                InitialModelState = $$"""{"{{nameof(ChildOnlyShelf.Status)}}":"open"}""",
                From = new Dictionary<Contracts.Events.EventType, Contracts.Projections.FromDefinition>
                {
                    [ToContract(createdEventType)] = new() { Key = WellKnownExpressions.EventSourceId, Properties = new Dictionary<string, string> { [nameof(ChildOnlyShelf.Name)] = namingPolicy.GetPropertyName(nameof(ChildOnlyShelfCreated.Name)) } }
                },
                Children = new Dictionary<string, Contracts.Projections.ChildrenDefinition>
                {
                    [nameof(ChildOnlyShelf.Books)] = new()
                    {
                        IdentifiedBy = nameof(ChildOnlyShelfBook.Isbn),
                        FromEventProperty = new() { Event = ToContract(shelvedEventType), PropertyExpression = namingPolicy.GetPropertyName(nameof(ChildOnlyBookShelved.Book)) }
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

            await EventStore.EventLog.Append(ShelfId, new ChildOnlyBookShelved(new ChildOnlyShelfBook("978-1", "Event Modeling")));
            DocumentAfterTheChildOnlyEvent = await StoredReadModelDocument.ReadWhen(
                ChronicleFixture,
                namingPolicy.GetReadModelName(typeof(ChildOnlyShelf)),
                document => StoredReadModelDocument.Field(document, "books") is BsonArray { Count: 1 });

            await EventStore.EventLog.Append(ShelfId, new ChildOnlyShelfCreated("Fiction"));
            DocumentAfterTheRootEvent = await StoredReadModelDocument.ReadWhen(
                ChronicleFixture,
                namingPolicy.GetReadModelName(typeof(ChildOnlyShelf)),
                document => StoredReadModelDocument.Field(document, "name") is { IsString: true });
        }

        static Contracts.Events.EventType ToContract(EventType eventType) => new()
        {
            Id = eventType.Id,
            Generation = eventType.Generation.Value,
            Tombstone = eventType.Tombstone
        };
    }

    [Fact] void should_have_read_the_stored_documents_when_the_backend_allows_it() =>
        (!Context.DocumentCanBeInspected || (Context.DocumentAfterTheChildOnlyEvent is not null && Context.DocumentAfterTheRootEvent is not null)).ShouldBeTrue();

    [Fact] void should_store_the_root_as_not_initialized_after_the_child_only_event() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.DocumentAfterTheChildOnlyEvent, "__initialized") is BsonBoolean { Value: false }).ShouldBeTrue();

    [Fact] void should_store_the_root_without_initial_values_after_the_child_only_event() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.DocumentAfterTheChildOnlyEvent, "status") is null).ShouldBeTrue();

    [Fact] void should_store_the_child_after_the_child_only_event() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.DocumentAfterTheChildOnlyEvent, "books") is BsonArray { Count: 1 }).ShouldBeTrue();

    [Fact] void should_initialize_the_root_when_a_root_event_arrives() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.DocumentAfterTheRootEvent, "__initialized") is BsonBoolean { Value: true }).ShouldBeTrue();

    [Fact] void should_fill_in_the_initial_values_when_a_root_event_arrives() =>
        (!Context.DocumentCanBeInspected || string.Equals(StoredReadModelDocument.Field(Context.DocumentAfterTheRootEvent, "status")?.ToString(), "open", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] void should_keep_the_child_when_a_root_event_arrives() =>
        (!Context.DocumentCanBeInspected || StoredReadModelDocument.Field(Context.DocumentAfterTheRootEvent, "books") is BsonArray { Count: 1 }).ShouldBeTrue();
}

[EventType]
public record ChildOnlyShelfCreated(string Name);

public record ChildOnlyShelfBook(string Isbn, string Title);

[EventType]
public record ChildOnlyBookShelved(ChildOnlyShelfBook Book);

public record ChildOnlyShelf(string Id, string Name, string Status, IEnumerable<ChildOnlyShelfBook> Books);

#pragma warning restore SA1402
