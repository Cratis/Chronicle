// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Assertions.for_ExpectedValueComparer;

public class when_comparing_value_contracts : Specification
{
    ValueDifference? _concept;
    ValueDifference? _primitive;
    ValueDifference? _dictionary;
    ValueDifference? _emptyObjects;
    ValueDifference? _enumerables;
    ValueDifference? _nestedPartial;

    void Because()
    {
        var id = new JoinCustomerId(Guid.Empty);
        _concept = ExpectedValueComparer.Compare(id, new JoinCustomerId(Guid.Empty), ValueComparison.Partial);
        _primitive = ExpectedValueComparer.Compare(id.Value, id, ValueComparison.Partial);
        _dictionary = ExpectedValueComparer.Compare(
            new Dictionary<string, object?> { ["b"] = 2, ["a"] = null }.ToFrozenDictionary(),
            new Dictionary<string, object?> { ["a"] = null, ["b"] = 2 },
            ValueComparison.Exact);
        _emptyObjects = ExpectedValueComparer.Compare(new object(), new object(), ValueComparison.Partial);
        _enumerables = ExpectedValueComparer.Compare(new[] { 1 }, new List<int> { 1 }, ValueComparison.Typed);
        _nestedPartial = ExpectedValueComparer.Compare(new[] { new { Name = "Ada" } }, new[] { new { Name = "Ada", Age = 42 } }, ValueComparison.Partial);
    }

    [Fact] void should_compare_concepts_by_typed_value() => _concept.ShouldBeNull();
    [Fact] void should_not_coerce_concepts_to_primitives() => _primitive.ShouldNotBeNull();
    [Fact] void should_compare_read_only_dictionary_entries_independently_of_order() => _dictionary.ShouldBeNull();
    [Fact] void should_not_equate_unrelated_propertyless_objects() => _emptyObjects.ShouldNotBeNull();
    [Fact] void should_check_runtime_type_even_for_enumerable_events() => _enumerables.ShouldNotBeNull();
    [Fact] void should_apply_partial_shapes_to_collection_elements() => _nestedPartial.ShouldBeNull();
}
