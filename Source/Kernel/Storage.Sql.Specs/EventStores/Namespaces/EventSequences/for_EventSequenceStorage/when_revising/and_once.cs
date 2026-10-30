// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_once : given.an_event_sequence_storage
{
    static readonly EventHash _originalHash = new("original-hash");
    static readonly EventHash _revisedHash = new("revised-hash");
    AppendedEvent _revisedEvent;
    EventEntry _stored;

    async Task Establish()
    {
        await Append(EventSequenceNumber.First, Content("original"), _originalHash);
    }

    async Task Because()
    {
        await _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, Content("revised"), _revisedHash);
        _revisedEvent = await _storage.GetEventAt(EventSequenceNumber.First);
        await using var context = CreateContext();
        _stored = context.Events.AsNoTracking().Single(_ => _.SequenceNumber == 0);
    }

    [Fact] void should_record_the_revision_generation() => _revisedEvent.RevisedGeneration.ShouldEqual(_eventType.Generation);
    [Fact] void should_identify_the_event_as_revised() => _revisedEvent.IsRevised.ShouldBeTrue();
    [Fact] void should_persist_the_revised_hash() => _revisedEvent.Context.Hash.ShouldEqual(_revisedHash);
    [Fact] void should_not_return_the_original_hash() => _revisedEvent.Context.Hash.ShouldNotEqual(_originalHash);
    [Fact] void should_return_the_revised_content() => ((IDictionary<string, object?>)_revisedEvent.Content)["value"].ShouldEqual("revised");

    [Fact] void should_record_one_revision() => EventEntryConverter.GetRevisions(_stored).Count.ShouldEqual(1);
    [Fact] void should_keep_the_original_content_untouched() => _stored.Content.Contains("original").ShouldBeTrue();
    [Fact] void should_expose_the_original_content() => _revisedEvent.OriginalContent.Contains("original").ShouldBeTrue();

    static ExpandoObject Content(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = value;
        return content;
    }
}
