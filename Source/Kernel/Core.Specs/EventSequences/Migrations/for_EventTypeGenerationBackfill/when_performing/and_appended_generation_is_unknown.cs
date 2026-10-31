// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_appended_generation_is_unknown : given.a_backfill
{
    void Establish()
    {
        _snapshot = _snapshot with { AppendedGeneration = null };
    }

    async Task Because() => await Perform();

    [Fact] void should_use_the_highest_base_generation() => AddedValue().ShouldEqual("derived");
    [Fact] void should_record_the_source_generation() => _additions[0].Provenance.Source.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_record_a_derived_source() => _additions[0].Provenance.SourceIsAppended.ShouldBeFalse();
}
