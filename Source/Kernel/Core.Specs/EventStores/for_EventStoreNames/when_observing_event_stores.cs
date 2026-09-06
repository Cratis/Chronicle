// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Json;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames;

/// <summary>
/// The names travel to the Workbench through a serializer that picks the collection converter from the
/// runtime type. A lazily projected sequence over <see cref="EventStoreName"/> still carries the concept as its
/// first generic argument, which makes the concept-collection converter claim a sequence of strings and blow up
/// on the way out - the in-memory backend hands over a list, which is exactly the shape that triggers it.
/// </summary>
public class when_observing_event_stores : Specification
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new EnumerableConceptAsJsonConverterFactory(),
            new ConceptAsJsonConverterFactory()
        }
    };

    IStorage _storage;
    IEnumerable<EventStoreNames> _emitted;
    string _json;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var stores = new ReplaySubject<IEnumerable<EventStoreName>>(1);
        stores.OnNext(new List<EventStoreName> { EventStoreName.System, "some-store" });
        _storage.ObserveEventStores().Returns(stores);
    }

    void Because()
    {
        EventStoreNames.ObserveEventStores(_storage).Subscribe(names => _emitted = names);
        _json = JsonSerializer.Serialize<object>(_emitted, _serializerOptions);
    }

    [Fact] void should_emit_every_name() => _emitted.Select(_ => _.Name).ShouldContainOnly(EventStoreName.System.Value, "some-store");
    [Fact] void should_serialize_the_name_of_each() => _json.ShouldEqual("""[{"id":"System","name":"System"},{"id":"some-store","name":"some-store"}]""");
}
