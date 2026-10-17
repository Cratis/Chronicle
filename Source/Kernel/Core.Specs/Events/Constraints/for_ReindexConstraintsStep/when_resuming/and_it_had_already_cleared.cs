// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_resuming;

public class and_it_had_already_cleared : given.a_handoff_reindex_step
{
    async Task Establish()
    {
        await Perform();
        _calls.Clear();
    }

    async Task Because() => await Perform();

    [Fact] void should_start_again_before_writing() => _calls.ShouldEqual(["starting", "clear"]);
}
