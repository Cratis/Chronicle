// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueAttributeExtensions.when_getting_constraint_name;

public class and_attribute_does_not_have_name : Specification
{
    string _result;

    void Because() => _result = typeof(SomeType).GetConstraintName();

    [Fact] void should_return_name_of_member() => _result.ShouldEqual(nameof(SomeType));

    [Unique]
    record SomeType();
}
