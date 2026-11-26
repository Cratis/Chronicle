// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asserting_read_models;

public class when_matching_a_partial_read_model : a_read_model_assertion
{
    Exception _error;

    void Establish() => _document.Next = new("Nested", 99m, [3], new Dictionary<string, JoinCustomerId?>());

    void Because() => _error = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new
    {
        Name = "Ada",
        Next = new { Name = "Nested" },
        Values = new List<int> { 1, 2 },
        Metadata = new Dictionary<string, object?> { ["note"] = null, ["owner"] = new JoinCustomerId(_customer.Value) }
    }));

    [Fact] void should_match_nested_shapes_and_concept_values() => _error.ShouldBeNull();
    [Fact] void should_keep_the_keyed_instance_available() => _scenario.InstanceForEventSourceId(_key)!.Name.ShouldEqual(_document.Name);
}
