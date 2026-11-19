// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_pinned_silo_holds_no_window_any_more : given.a_window_mismatch
{
    readonly Guid _first = Guid.NewGuid();
    readonly Guid _second = Guid.NewGuid();

    async Task Because() => await Mismatch(Windows((SiloAddress.Zero, [_first, _second])), Windows((SiloAddress.Zero, [])));

    [Fact] void should_list_every_removed_window_in_ascending_order() => Message.ShouldContain($"removed {string.Join(", ", new[] { _first, _second }.Order())}");
    [Fact] void should_say_the_silo_returned_nothing() => Message.ShouldContain("returned <none>");
    [Fact] void should_not_name_the_silo_as_missing() => Message.ShouldContain("missing silos <none>");
}
