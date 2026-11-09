// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildrenSortValues.when_comparing;

public class numbers_of_different_types : Specification
{
    [Fact] void should_compare_an_int_and_a_long_numerically() => ChildrenSortValues.Compare(2, 10L).ShouldBeLessThan(0);
    [Fact] void should_compare_a_double_and_a_decimal_numerically() => ChildrenSortValues.Compare(2.5d, 2m).ShouldBeGreaterThan(0);
    [Fact] void should_consider_equal_values_of_different_types_equal() => ChildrenSortValues.Compare(3, 3.0d).ShouldEqual(0);
}
