// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_value_moves_between_sources : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("holder", _definition, 0, Hash("value"));
        Event(1, "claimant", "Claimed", "value");
        Event(2, "holder", "Removed");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_release_before_claiming() => _writes[0].ShouldEqual("release:holder");
    [Fact] async Task should_transfer_the_value() => (await Values("claimant")).ShouldContainOnly(Hash("value").Value);
    [Fact] void should_settle_no_refusal() => _result.Settled.ShouldEqual(0);
}
