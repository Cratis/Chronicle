// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_subject_was_erased : given.a_replay
{
    void Establish()
    {
        Event(0, "erased", "Claimed", string.Empty);
        Event(1, "readable", "Claimed", "readable");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_not_claim_an_unreadable_value() => (await Values("erased")).ShouldBeEmpty();
    [Fact] async Task should_continue_with_other_subjects() => (await Values("readable")).ShouldContainOnly(Hash("readable").Value);
}
