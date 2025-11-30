// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.when_calling_with_ireadonlylist;

public class with_empty_collection : given.a_cluster_with_serialization_test_grain
{
    IReadOnlyList<string> _result;

    async Task Because() => _result = await _grain.TestIReadOnlyList([]);

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
