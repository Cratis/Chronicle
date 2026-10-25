// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_stored_generations;

public class and_an_older_node_redacted_a_revised_event : given.a_storage_with_stored_generations
{
    StoredEventGenerations _snapshot;
    string? _storedRevisions;

    async Task Establish()
    {
        await ReviseWithPersonalData();
        await RedactAsAnOlderNode();
        _storedRevisions = Stored(0).Revisions;
    }

    async Task Because()
    {
        _snapshot = (await _storage.GetStoredGenerations(0))!;
    }

    [Fact] void should_still_hold_the_revision_the_older_node_did_not_know_about() => _storedRevisions.ShouldNotBeNull();
    [Fact] void should_report_the_redaction_marker() => _snapshot.EventTypeId.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_report_no_revisions() => _snapshot.RevisionCount.ShouldEqual(0);
    [Fact] void should_not_expose_the_revised_content() => _snapshot.RevisedContent.ShouldBeNull();
    [Fact] void should_not_report_a_revised_generation() => _snapshot.RevisedGeneration.ShouldBeNull();
    [Fact] void should_not_carry_the_revised_content_in_the_concurrency_token() => _snapshot.ConcurrencyToken.Contains("personal").ShouldBeFalse();
}
