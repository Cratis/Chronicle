// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_processing_fails : a_projection_slice
{
    Exception _error;
    Exception _then;
    Exception _repeat;

    void Establish() => _slice.GivenEvents(_source, new TallyBroke());

    async Task Because()
    {
        _error = await Catch.Exception(_slice.Run);
        _then = Catch.Exception(() => _ = _slice.Then);
        _repeat = await Catch.Exception(_slice.Run);
    }

    [Fact] void should_preserve_the_reducer_failure() => _error.ShouldBeOfExactType<ReducerFailed>();
    [Fact] void should_preserve_the_reducer_message() => _error.Message.ShouldContain("Reducer intentionally failed.");
    [Fact] void should_leave_then_unavailable() => _then.ShouldBeOfExactType<SliceNotRun>();
    [Fact] void should_not_retry_the_failed_run() => _repeat.ShouldBeOfExactType<SliceAlreadyRun>();
}
