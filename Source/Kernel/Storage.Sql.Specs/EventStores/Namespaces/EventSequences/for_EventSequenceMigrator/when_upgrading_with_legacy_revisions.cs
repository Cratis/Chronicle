// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

/// <summary>
/// Proves the upgrade of a table holding events revised in place by the old implementation: the new column is added,
/// the legacy rows still read as before, and a later revision keeps only what is still stored as the original.
/// </summary>
public abstract class when_upgrading_with_legacy_revisions : given.a_sequence_with_legacy_revisions
{
    IReadOnlyList<string?> _revisionsColumn = [];
    AppendedEvent _plainBefore;
    AppendedEvent _revisedAtOneBefore;
    AppendedEvent _revisedAtTwoBefore;
    AppendedEvent _plainAfter;
    AppendedEvent _revisedAtOneAfter;
    AppendedEvent _revisedAtTwoAfter;

    async Task Because()
    {
        await using (var context = CreateContext())
        {
            await context.EnsureTableExists();
        }

        // The migration cache must be cleared so the second call really runs the column check and upgrade again.
        _migrator.ClearMigrationCache(_connectionString);
        await using (var context = CreateContext())
        {
            await context.EnsureTableExists();
            _revisionsColumn = await context.Events.AsNoTracking().OrderBy(e => e.SequenceNumber).Select(e => e.Revisions).ToListAsync();
        }

        _plainBefore = await _storage.GetEventAt(PlainEvent);
        _revisedAtOneBefore = await _storage.GetEventAt(EventRevisedAtGenerationOne);
        _revisedAtTwoBefore = await _storage.GetEventAt(EventRevisedAtGenerationTwo);

        await Revise(PlainEvent, FirstGeneration, "after-plain", "hash-after-plain");
        await Revise(EventRevisedAtGenerationOne, FirstGeneration, "after-one", "hash-after-one");
        await Revise(EventRevisedAtGenerationTwo, SecondGeneration, "after-two", "hash-after-two");

        _plainAfter = await _storage.GetEventAt(PlainEvent);
        _revisedAtOneAfter = await _storage.GetEventAt(EventRevisedAtGenerationOne);
        _revisedAtTwoAfter = await _storage.GetEventAt(EventRevisedAtGenerationTwo);
    }

    [Fact] public void should_add_the_revisions_column_without_values() => _revisionsColumn.Count.ShouldEqual(3);
    [Fact] public void should_leave_every_legacy_revisions_value_null() => _revisionsColumn.Any(_ => _ is not null).ShouldBeFalse();
    [Fact] public void should_read_the_plain_event_as_before() => Json(_plainBefore.Content).ShouldEqual("""{"value":"original"}""");
    [Fact] public void should_read_the_plain_event_without_revisions() => _plainBefore.Revisions.Count().ShouldEqual(0);
    [Fact] public void should_read_the_plain_event_without_original_content() => _plainBefore.OriginalContent.ShouldEqual(string.Empty);
    [Fact] public void should_read_the_legacy_event_revised_at_generation_one_with_its_revised_content() => Json(_revisedAtOneBefore.Content).ShouldEqual("""{"value":"legacy-one"}""");
    [Fact] public void should_read_the_legacy_event_revised_at_generation_one_with_its_revised_hash() => _revisedAtOneBefore.Context.Hash.ShouldEqual(new EventHash("hash-legacy-one"));
    [Fact] public void should_read_the_legacy_event_revised_at_generation_one_without_revisions() => _revisedAtOneBefore.Revisions.Count().ShouldEqual(0);
    [Fact] public void should_read_the_legacy_event_revised_at_generation_one_without_original_content() => _revisedAtOneBefore.OriginalContent.ShouldEqual(string.Empty);
    [Fact] public void should_read_the_legacy_event_revised_at_generation_two_with_its_revised_content() => Json(_revisedAtTwoBefore.Content).ShouldEqual("""{"value":"legacy-two"}""");
    [Fact] public void should_read_the_legacy_event_revised_at_generation_two_without_revisions() => _revisedAtTwoBefore.Revisions.Count().ShouldEqual(0);
    [Fact] public void should_read_the_legacy_event_revised_at_generation_two_without_original_content() => _revisedAtTwoBefore.OriginalContent.ShouldEqual(string.Empty);
    [Fact] public void should_revise_the_plain_event_once() => _plainAfter.Revisions.Count().ShouldEqual(1);
    [Fact] public void should_keep_the_plain_events_original() => SameJson(_plainAfter.OriginalContent, "original");
    [Fact] public void should_deliver_the_plain_events_revised_content() => Json(_plainAfter.Content).ShouldEqual("""{"value":"after-plain"}""");
    [Fact] public void should_revise_the_legacy_event_at_generation_one_with_exactly_one_revision() => _revisedAtOneAfter.Revisions.Count().ShouldEqual(1);
    [Fact] public void should_expose_the_overwritten_content_as_the_original_at_generation_one() => SameJson(_revisedAtOneAfter.OriginalContent, "legacy-one");
    [Fact] public void should_deliver_the_new_revision_at_generation_one() => Json(_revisedAtOneAfter.Content).ShouldEqual("""{"value":"after-one"}""");
    [Fact] public void should_revise_the_legacy_event_at_generation_two_with_exactly_one_revision() => _revisedAtTwoAfter.Revisions.Count().ShouldEqual(1);
    [Fact] public void should_keep_the_untouched_generation_one_as_the_original_at_generation_two() => SameJson(_revisedAtTwoAfter.OriginalContent, "original");
    [Fact] public void should_deliver_the_new_revision_at_generation_two() => Json(_revisedAtTwoAfter.Content).ShouldEqual("""{"value":"after-two"}""");
}
