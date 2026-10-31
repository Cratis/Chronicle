// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_appended_content_is_missing : given.a_backfill
{
    void Establish()
    {
        _snapshot = _snapshot with { Content = _snapshot.Content.Where(_ => _.Key != EventTypeGeneration.First).ToDictionary() };
    }

    async Task Because() => await Perform();

    [Fact] void should_use_the_highest_remaining_base_generation() => AddedValue().ShouldEqual("derived");
    [Fact] void should_not_claim_it_is_the_appended_source() => _additions[0].Provenance.SourceIsAppended.ShouldBeFalse();
}
