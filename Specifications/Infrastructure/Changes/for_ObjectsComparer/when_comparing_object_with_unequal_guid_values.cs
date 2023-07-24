// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_object_with_unequal_guid_values : given.an_object_comparer
{
    record TheType(Guid TheGuid);

    TheType left;
    TheType right;

    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(Guid.NewGuid());
        right = new(Guid.NewGuid());
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_not_be_equal() => result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => differences.Count().ShouldEqual(1);
    [Fact] void should_have_concept_property_as_difference() => differences.First().PropertyPath.Path.ShouldEqual(nameof(TheType.TheGuid).ToCamelCase());
}
