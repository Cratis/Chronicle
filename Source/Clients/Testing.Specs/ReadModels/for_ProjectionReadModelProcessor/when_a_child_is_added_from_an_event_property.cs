// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor;

public class when_a_child_is_added_from_an_event_property : Specification
{
    EventStoreForTesting _store;
    EventSourceId _shelfId;
    Shelf? _instance;

    void Establish()
    {
        _store = new EventStoreForTesting();
        _shelfId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because()
    {
        var bookShelved = _store.EventTypes.GetEventTypeFor(typeof(BookShelved));
        var definition = new Contracts.Projections.ProjectionDefinition
        {
            Identifier = Guid.NewGuid().ToString(),
            ReadModel = typeof(Shelf).FullName!,
            IsActive = true,
            InitialModelState = "{}",
            Children = new Dictionary<string, Contracts.Projections.ChildrenDefinition>
            {
                ["books"] = new()
                {
                    IdentifiedBy = "isbn",
                    FromEventProperty = new()
                    {
                        Event = new() { Id = bookShelved.Id, Generation = bookShelved.Generation.Value, Tombstone = bookShelved.Tombstone },
                        PropertyExpression = "book"
                    }
                }
            }
        };

        var (_, instances) = await ProjectionReadModelProcessor.Process<Shelf>(
            definition,
            [(_shelfId, new BookShelved(new ShelfBook("978-1", "Event Modeling")))],
            _store.EventTypes,
            _store.EventSerializer,
            _store.JsonSchemaGenerator);
        instances.TryGetValue(_shelfId, out _instance);
    }

    [Fact] void should_create_the_instance() => _instance.ShouldNotBeNull();
    [Fact] void should_add_the_child() => _instance!.Books.Single().Title.ShouldEqual("Event Modeling");
}
