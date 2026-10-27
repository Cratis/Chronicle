// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_child_only_root_loses_one_child : given.a_projection_grain_with_a_children_projection
{
    IEnumerable<ExpandoObject> _result;

    async Task Because() => _result = await _grain.Process(
        EventStoreNamespaceName.Default,
        [
            Associated("issue", 1, "remaining-comment", "100"),
            Associated("issue", 2, "removed-comment", "200"),
            Removed("issue", 3, "removed-comment")
        ]);

    [Fact] void should_return_one_read_model() => _result.Count().ShouldEqual(1);
    [Fact] void should_keep_the_remaining_child() => AssociationsOf(_result.Single()).Select(_ => _["id"]).ShouldContainOnly("remaining-comment");
    [Fact] void should_initialize_the_root() => ((IDictionary<string, object?>)_result.Single())[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
}
