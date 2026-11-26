// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_disposed : a_projection_slice
{
    Exception _run;
    Exception _given;
    Exception _then;

    void Establish() => _slice.Dispose();

    async Task Because()
    {
        _run = await Catch.Exception(_slice.Run);
        _given = Catch.Exception(() => _slice.GivenEvents(_source, new Tallied()));
        _then = Catch.Exception(() => _ = _slice.Then);
    }

    [Fact] void should_reject_execution() => _run.ShouldBeOfExactType<SliceDisposed>();
    [Fact] void should_reject_inputs() => _given.ShouldBeOfExactType<SliceDisposed>();
    [Fact] void should_reject_outcome_access() => _then.ShouldBeOfExactType<SliceDisposed>();
}
