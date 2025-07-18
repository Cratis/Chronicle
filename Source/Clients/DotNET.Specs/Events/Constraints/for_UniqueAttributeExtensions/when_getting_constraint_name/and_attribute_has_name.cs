// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueAttributeExtensions.when_getting_constraint_name;

public class and_attribute_has_name : Specification
{
    const string Name = "TheName";
    string _result;

    void Because() => _result = typeof(SomeType).GetConstraintName();

    [Fact] void should_return_name_of_member() => _result.ShouldEqual(Name);

    [Unique(Name)]
    record SomeType();
}
