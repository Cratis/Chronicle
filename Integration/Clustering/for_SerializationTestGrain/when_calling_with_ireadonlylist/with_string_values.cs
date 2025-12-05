// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.when_calling_with_ireadonlylist;

public class with_string_values : given.a_cluster_with_serialization_test_grain
{
    IReadOnlyList<string> _input;
    IReadOnlyList<string> _result;

    void Establish() => _input = new List<string> { "one", "two", "three" }.AsReadOnly();

    async Task Because() => _result = await _grain.TestIReadOnlyList(_input);

    [Fact] void should_return_same_values() => _result.ShouldContainOnly(_input);
}
