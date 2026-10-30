// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexes;

public class when_resolving_nested_paths_with_a_naming_policy : Specification
{
    [CompositeIndex("Address.City", nameof(Status), IsDescending = new[] { true, false })]
    record Model(Location Address, string Status);
    record Location(string City);

    INamingPolicy _policy;
    List<IndexDefinition> _indexes;

    void Establish()
    {
        _policy = Substitute.For<INamingPolicy>();
        _policy.GetPropertyName(Arg.Any<string>()).Returns(call => JsonNamingPolicy.CamelCase.ConvertName(call.Arg<string>()));
    }

    void Because() => _indexes = new ReadModelIndexes(_policy).For(typeof(Model));

    [Fact] void should_normalize_each_path_segment() => _indexes[0].Properties.Select(property => property.Path).SequenceEqual(["address.city", "status"]).ShouldBeTrue();
    [Fact] void should_keep_the_leading_path_for_older_kernels() => _indexes[0].PropertyPath.ShouldEqual("address.city");
    [Fact] void should_keep_directions_in_property_order() => _indexes[0].Properties.Select(property => property.Descending).SequenceEqual([true, false]).ShouldBeTrue();
}
