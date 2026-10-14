// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_removal_was_missed : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("source", _definition, 0, Hash("value"));
        Event(1, "source", "Removed");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_release_the_stored_claim() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_write_only_the_release() => _writes.ShouldContainOnly("release:source");
}
