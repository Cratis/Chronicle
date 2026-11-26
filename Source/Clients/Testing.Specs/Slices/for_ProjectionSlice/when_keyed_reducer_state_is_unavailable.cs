// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices.for_ProjectionSlice;

public class when_keyed_reducer_state_is_unavailable : a_projection_slice
{
    Exception _partial;
    Exception _exact;
    Exception _absence;

    void Establish() => _slice.GivenEvents("first", new Tallied()).GivenEvents("second", new Tallied());

    async Task Because()
    {
        var outcome = await _slice.Run();
        _partial = Catch.Exception(() => outcome.ShouldPartiallyMatchReadModel("first", new { Count = 2 }));
        _exact = Catch.Exception(() => outcome.ShouldExactlyMatchReadModel("first", new { Id = Guid.Empty, Count = 2 }));
        _absence = Catch.Exception(() => outcome.ShouldHaveNoReadModel("missing"));
    }

    [Fact] void should_keep_existing_unkeyed_reducer_behavior()
    {
        _scenario.Instance.ShouldNotBeNull();
        _scenario.Instances.Count.ShouldEqual(0);
    }
    [Fact] void should_not_assign_the_primary_to_an_arbitrary_key() => _partial.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_not_compare_an_unkeyed_primary_exactly() => _exact.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_fail_closed_instead_of_claiming_absence() => _absence.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_explain_the_per_key_seed_alternative() => _absence.Message.ShouldContain("per-key seeds");
}
