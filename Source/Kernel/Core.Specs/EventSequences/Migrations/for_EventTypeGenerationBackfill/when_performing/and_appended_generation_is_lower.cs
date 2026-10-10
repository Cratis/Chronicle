// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_appended_generation_is_lower : given.a_backfill
{
    async Task Because() => await Perform();

    [Fact] void should_use_the_original_source() => AddedValue().ShouldEqual("original");
    [Fact] void should_only_add_the_missing_generation() => _additions.Select(_ => _.Generation).ShouldContainOnly(new EventTypeGeneration(3));
    [Fact] void should_record_the_appended_source() => _additions[0].Provenance.SourceIsAppended.ShouldBeTrue();
}
