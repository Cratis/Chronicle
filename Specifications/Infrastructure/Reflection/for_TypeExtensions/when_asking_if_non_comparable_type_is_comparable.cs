// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reflection.for_TypeExtensions;

public class when_asking_if_non_comparable_type_is_comparable : Specification
{
    bool result;

    void Because() => result = typeof(object).IsComparable();

    [Fact] void should_not_be_considered_comparable() => result.ShouldBeFalse();
}
