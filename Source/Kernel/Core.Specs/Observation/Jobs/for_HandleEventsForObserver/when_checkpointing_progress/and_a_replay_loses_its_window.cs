// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_replay_loses_its_window : given.a_window_mismatch
{
    async Task Because() => await Mismatch(Windows((SiloAddress.Zero, [Guid.NewGuid()])), Windows());

    [Fact] void should_say_it_was_a_replay() => Message.ShouldContain("mode replay");
}
