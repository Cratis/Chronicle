// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.when_calling_with_concept_as;

public class with_custom_value : given.a_cluster_with_serialization_test_grain
{
    Chronicle.Concepts.EventStoreName _input;
    Chronicle.Concepts.EventStoreName _result;

    void Establish() => _input = new Chronicle.Concepts.EventStoreName("TestStore");

    async Task Because() => _result = await _grain.TestConceptAs(_input);

    [Fact] void should_return_same_value() => _result.ShouldEqual(_input);
}
