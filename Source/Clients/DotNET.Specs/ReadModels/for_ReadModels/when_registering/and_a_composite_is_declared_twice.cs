// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class and_a_composite_is_declared_twice : Specification
{
    [CompositeIndex(nameof(Name), nameof(Other))]
    [CompositeIndex(nameof(Name), nameof(Other))]
    record Model(string Name, string Other);

    [CompositeIndex(nameof(Name), nameof(Other))]
    [CompositeIndex(nameof(Other), nameof(Name))]
    [CompositeIndex(nameof(Name), nameof(Other), IsDescending = [true, false])]
    record DifferentDefinitions(string Name, string Other);

    [CompositeIndex(nameof(Name), nameof(Other))]
    record ModelBase(string Name, string Other);
    [CompositeIndex(nameof(Name), nameof(Other))]
    record InheritedModel(string Name, string Other) : ModelBase(Name, Other);

    Contracts.ReadModels.IndexDefinition[] _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model)).ToArray();

    [Fact] void should_register_the_definition_once() => _indexes.Length.ShouldEqual(1);
    [Fact] void should_coalesce_inherited_declarations() => new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(InheritedModel)).Count.ShouldEqual(1);
    [Fact] void should_keep_distinct_orders_and_directions() => new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(DifferentDefinitions)).Count.ShouldEqual(3);
    [Fact] void should_preserve_the_ordered_keys() => _indexes[0].Properties.Select(property => property.Path).ShouldEqual(["Name", "Other"]);
}
