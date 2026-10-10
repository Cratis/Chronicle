// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_event_is_revised : given.a_backfill
{
    void Establish()
    {
        _snapshot = _snapshot with { RevisionCount = 1 };
    }

    async Task Because() => await Perform();

    [Fact] void should_use_base_content_not_delivery_content() => AddedValue().ShouldEqual("original");
    [Fact] void should_guard_the_revision_count() => _writes[0].RevisionCount.ShouldEqual(1);
}
