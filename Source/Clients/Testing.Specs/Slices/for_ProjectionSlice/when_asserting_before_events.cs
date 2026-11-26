// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_asserting_before_events : a_projection_slice
{
    Exception _then;

    async Task Because()
    {
        _then = Catch.Exception(() => _ = _slice.Then);
        var outcome = await _slice.Run();
        outcome.ShouldHaveNoReadModel(_source);
    }

    [Fact] void should_require_a_successful_run() => _then.ShouldBeOfExactType<SliceNotRun>();
    [Fact] void should_allow_an_empty_successful_run() => _slice.Then.ShouldNotBeNull();
}
