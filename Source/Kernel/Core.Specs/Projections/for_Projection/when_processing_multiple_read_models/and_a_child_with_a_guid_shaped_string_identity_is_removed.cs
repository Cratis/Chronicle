// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_child_with_a_guid_shaped_string_identity_is_removed : given.a_projection_grain_with_a_children_projection
{
    const string Remaining = "3f2b1234-1234-1234-1234-123456789abc";
    const string RemovedChild = "3f2b5678-5678-5678-5678-123456789abc";

    IEnumerable<ExpandoObject> _result;

    async Task Because() => _result = await _grain.Process(
        EventStoreNamespaceName.Default,
        [
            Associated("issue", 1, Remaining, "100"),
            Associated("issue", 2, RemovedChild, "200"),
            Removed("issue", 3, RemovedChild)
        ]);

    [Fact] void should_return_one_read_model() => _result.Count().ShouldEqual(1);
    [Fact] void should_keep_only_the_remaining_child() => AssociationsOf(_result.Single()).Select(_ => _["id"]).ShouldContainOnly(Remaining);
}
