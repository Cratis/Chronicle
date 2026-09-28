// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

/// <summary>
/// A fold that throws must fail only itself. The projection engine is shared by every read of the projection and
/// dispatches through Rx subscriptions, which dispose themselves when a handler throws - so a single failed fold
/// used to leave the engine projecting nothing, and every later read of every instance returned no read model
/// until the grain was rebuilt (Cratis/Chronicle#4337). The failure here is the one seen in production: a child
/// that came back from another silo as a dictionary, which cannot be looked up by identity.
/// </summary>
public class and_an_earlier_fold_failed : given.a_projection_grain_with_a_children_projection
{
    Exception _failure;
    ExpandoObject _result;

    async Task Because()
    {
        var stateFromAnotherSilo = new ExpandoObject();
        ((IDictionary<string, object?>)stateFromAnotherSilo)["associations"] = new List<object>
        {
            new Dictionary<string, object?> { ["id"] = "comment-1", ["reference"] = "100" }
        };

        _failure = await Catch.Exception(() => FoldOnto(stateFromAnotherSilo, Associated("issue", 3, "comment-1", "101")));
        _result = await Fold(Associated("another-issue", 4, "comment-3", "300"));
    }

    [Fact] void should_fail_the_fold_it_could_not_do() => _failure.ShouldNotBeNull();
    [Fact] void should_still_project_a_fold_that_follows() => AssociationsOf(_result).Single()["reference"].ShouldEqual("300");
}
