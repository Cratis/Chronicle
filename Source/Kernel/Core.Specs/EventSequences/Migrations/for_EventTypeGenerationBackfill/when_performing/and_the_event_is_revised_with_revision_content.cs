// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_event_is_revised_with_revision_content : given.a_backfill
{
    void Establish()
    {
        _snapshot = _snapshot with
        {
            RevisionCount = 1,
            RevisedGeneration = 1,
            RevisedContent = """{"value":"revised","details":"keep me"}"""
        };
    }

    async Task Because() => await Perform();

    [Fact] void should_derive_from_the_revision() => AddedValue().ShouldEqual("revised");
    [Fact] void should_record_the_revision_generation_as_source() => _additions[0].Provenance.Source.ShouldEqual(new EventTypeGeneration(1));
    [Fact] void should_not_mark_the_source_as_appended() => _additions[0].Provenance.SourceIsAppended.ShouldBeFalse();
    [Fact] void should_guard_the_revision_count() => _writes[0].RevisionCount.ShouldEqual(1);
}
