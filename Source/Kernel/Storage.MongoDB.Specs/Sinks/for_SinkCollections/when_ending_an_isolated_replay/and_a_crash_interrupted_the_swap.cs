// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_ending_an_isolated_replay;

public class and_a_crash_interrupted_the_swap : given.an_isolated_replay
{
    void Establish() => _names.UnionWith(["Revert", "isolated-promoting"]);
    async Task Because() => await _collections.EndReplay(_context);
    [Fact] void should_finish_the_claimed_swap() => _names.ShouldContain("Model");
    [Fact] void should_preserve_the_original() => _names.ShouldContain("Revert");
    [Fact] void should_leave_no_incomplete_claim() => _names.ShouldNotContain("isolated-promoting");
}
