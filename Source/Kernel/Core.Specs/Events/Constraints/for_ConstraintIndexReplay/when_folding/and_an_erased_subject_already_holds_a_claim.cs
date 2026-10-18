// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_an_erased_subject_already_holds_a_claim : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("source", _definition, 0, Hash("live"));
        Event(0, "source", "Claimed", string.Empty);
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_skip_the_unreadable_input_without_mutating_the_index() => _writes.ShouldBeEmpty();
    [Fact] async Task should_preserve_the_existing_claim() => (await Values("source")).ShouldContainOnly(Hash("live").Value);
}
