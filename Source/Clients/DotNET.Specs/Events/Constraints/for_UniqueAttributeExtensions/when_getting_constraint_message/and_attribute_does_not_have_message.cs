// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueAttributeExtensions.when_getting_constraint_message;

public class and_attribute_does_not_have_message : Specification
{
    string _result;

    void Because() => _result = typeof(SomeType).GetConstraintMessage();

    [Fact] void should_return_empty_message() => _result.ShouldEqual(string.Empty);

    [Unique]
    record SomeType();
}
