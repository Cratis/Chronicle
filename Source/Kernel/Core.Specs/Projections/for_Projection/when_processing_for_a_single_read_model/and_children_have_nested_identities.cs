// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_children_have_nested_identities : given.a_projection_grain_with_a_children_projection
{
    ExpandoObject _result;

    protected override PropertyPath ChildIdentity => "identity.id";

    async Task Because() => _result = await Fold(
        Associated("issue", 1, "A", "100"),
        Associated("issue", 2, "B", "200"),
        Associated("issue", 3, "A", "101"));

    [Fact] void should_hold_each_child_once() => AssociationsOf(_result).Select(IdentityOf).ShouldContainOnly("A", "B");
    [Fact] void should_update_the_repeated_child() => AssociationsOf(_result).Single(child => Equals(IdentityOf(child), "A"))["reference"].ShouldEqual("101");
    [Fact] void should_not_write_the_identity_at_the_root() => AssociationsOf(_result).Any(child => child.ContainsKey("id")).ShouldBeFalse();

    static object? IdentityOf(IDictionary<string, object?> child) => ((IDictionary<string, object?>)child["identity"]!)["id"];
}
