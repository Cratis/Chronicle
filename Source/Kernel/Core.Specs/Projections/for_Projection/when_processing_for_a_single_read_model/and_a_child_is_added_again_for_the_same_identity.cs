// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_a_child_is_added_again_for_the_same_identity : given.a_projection_grain_with_a_children_projection
{
    ExpandoObject _result;

    async Task Because() => _result = await Fold(
        Associated("issue", 1, "comment-1", "100"),
        Associated("issue", 2, "comment-2", "200"),
        Associated("issue", 3, "comment-1", "101"));

    [Fact] void should_hold_each_child_once() => AssociationsOf(_result).Select(_ => _["id"]).ShouldContainOnly("comment-1", "comment-2");
    [Fact] void should_update_the_repeated_child() => AssociationsOf(_result).Single(_ => Equals(_["id"], "comment-1"))["reference"].ShouldEqual("101");
}
