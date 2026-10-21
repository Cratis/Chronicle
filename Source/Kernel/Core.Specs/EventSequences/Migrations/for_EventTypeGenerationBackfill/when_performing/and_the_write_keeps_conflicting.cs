// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_write_keeps_conflicting : given.a_backfill
{
    void Establish()
    {
        _writeResult = _ => false;
    }

    async Task Because() => await Perform();

    [Fact] void should_stop_after_one_retry() => _writes.Count.ShouldEqual(2);
}
