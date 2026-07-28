// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_revising_an_event : given.a_storage_with_appended_events
{
    AppendedEvent _stored;

    async Task Because()
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = "revised";

        await _storage.Revise(1, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, content, EventHash.NotSet);
        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)1UL);
    }

    [Fact] void should_replace_the_stored_content() => ((IDictionary<string, object?>)_stored.Content)["value"].ShouldEqual("revised");
    [Fact] void should_mark_the_event_as_revised() => _stored.IsRevised.ShouldBeTrue();
    [Fact] void should_record_a_single_revision() => _stored.Revisions.Count().ShouldEqual(1);
    [Fact] void should_keep_the_original_content() => _stored.OriginalContent.ShouldNotBeEmpty();
}
