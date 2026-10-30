// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_an_older_node_redacted_a_revised_event : given.a_storage_with_stored_generations
{
    AppendedEvent _read;

    async Task Establish()
    {
        await ReviseWithPersonalData();
        await RedactAsAnOlderNode();
    }

    async Task Because()
    {
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_read_the_redaction_type() => _read.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_not_be_revised() => _read.IsRevised.ShouldBeFalse();
    [Fact] void should_expose_no_revisions() => _read.Revisions.Count().ShouldEqual(0);
    [Fact] void should_expose_no_original_content() => _read.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_not_deliver_the_revised_content() => JsonSerializer.Serialize(_read.Content).Contains("personal").ShouldBeFalse();
}
