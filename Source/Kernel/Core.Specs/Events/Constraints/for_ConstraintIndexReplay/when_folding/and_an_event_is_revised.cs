// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_an_event_is_revised : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("source", _definition, 0, Hash("original"));
        Event(0, "source", "Claimed", "original");
        ((IDictionary<string, object?>)_events[0].Content)["email"] = "revised";
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_fold_the_revised_stored_content() => (await Values("source")).ShouldContainOnly(Hash("revised").Value);
}
