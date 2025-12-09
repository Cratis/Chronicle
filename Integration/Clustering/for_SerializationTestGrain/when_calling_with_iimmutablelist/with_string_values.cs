// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.when_calling_with_iimmutablelist;

public class with_string_values : given.a_cluster_with_serialization_test_grain
{
    IImmutableList<string> _input;
    IImmutableList<string> _result;

    void Establish() => _input = ImmutableList.Create("one", "two", "three");

    async Task Because() => _result = await _grain.TestIImmutableList(_input);

    [Fact] void should_return_same_values() => _result.ShouldContainOnly(_input);
}
