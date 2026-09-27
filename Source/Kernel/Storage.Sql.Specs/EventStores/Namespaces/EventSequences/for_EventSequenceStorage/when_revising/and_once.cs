// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_once : given.an_event_sequence_storage
{
    static readonly EventHash _originalHash = new("original-hash");
    static readonly EventHash _revisedHash = new("revised-hash");
    AppendedEvent _revisedEvent;

    async Task Establish()
    {
        await Append(EventSequenceNumber.First, Content("original"), _originalHash);
    }

    async Task Because()
    {
        await _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, Content("revised"), _revisedHash);
        _revisedEvent = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_persist_the_revised_hash() => _revisedEvent.Context.Hash.ShouldEqual(_revisedHash);
    [Fact] void should_not_return_the_original_hash() => _revisedEvent.Context.Hash.ShouldNotEqual(_originalHash);
    [Fact] void should_return_the_revised_content() => ((IDictionary<string, object?>)_revisedEvent.Content)["value"].ShouldEqual("revised");

    static ExpandoObject Content(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = value;
        return content;
    }
}
