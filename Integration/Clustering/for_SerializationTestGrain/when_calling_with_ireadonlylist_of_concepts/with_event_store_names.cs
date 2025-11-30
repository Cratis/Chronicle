// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.when_calling_with_ireadonlylist_of_concepts;

public class with_event_store_names : given.a_cluster_with_serialization_test_grain
{
    IReadOnlyList<Chronicle.Concepts.EventStoreName> _input;
    IReadOnlyList<Chronicle.Concepts.EventStoreName> _result;

    void Establish() => _input = new List<Chronicle.Concepts.EventStoreName> { new("Store1"), new("Store2"), Chronicle.Concepts.EventStoreName.System }.AsReadOnly();

    async Task Because() => _result = await _grain.TestIReadOnlyListOfConcept(_input);

    [Fact] void should_return_same_values() => _result.ShouldContainOnly(_input);
}
