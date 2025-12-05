// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.Integration.for_SerializationTestGrain.when_calling_with_concept_as;

public class with_static_value : given.a_cluster_with_serialization_test_grain
{
    Chronicle.Concepts.EventStoreName _result;

    async Task Because() => _result = await _grain.TestConceptAs(Chronicle.Concepts.EventStoreName.System);

    [Fact] void should_return_system_event_store() => _result.ShouldEqual(Chronicle.Concepts.EventStoreName.System);
}
