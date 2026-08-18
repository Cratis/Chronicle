// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Json;

namespace Cratis.Chronicle.Namespaces.for_NamespaceNames;

/// <summary>
/// Same guard as for the event store names: what leaves the read model must be a materialized string
/// collection, not a lazy projection the JSON converters can misread.
/// </summary>
public class when_observing_namespaces : Specification
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters =
        {
            new EnumerableConceptAsJsonConverterFactory(),
            new ConceptAsJsonConverterFactory()
        }
    };

    IStorage _storage;
    IEnumerable<string> _emitted;
    string _json;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var namespaces = new ReplaySubject<IEnumerable<NamespaceState>>(1);
        namespaces.OnNext(new List<NamespaceState>
        {
            new(EventStoreNamespaceName.Default, DateTimeOffset.UtcNow),
            new("tenant-a", DateTimeOffset.UtcNow)
        });
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Namespaces.ObserveAll().Returns(namespaces);
    }

    void Because()
    {
        NamespaceNames.ObserveNamespaces("some-store", _storage).Subscribe(names => _emitted = names);
        _json = JsonSerializer.Serialize<object>(_emitted, _serializerOptions);
    }

    [Fact] void should_emit_the_names_as_strings() => _emitted.ShouldContainOnly(EventStoreNamespaceName.Default.Value, "tenant-a");
    [Fact] void should_serialize_as_a_plain_string_array() => _json.ShouldEqual("""["Default","tenant-a"]""");
}
