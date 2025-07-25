// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_using_operator_to_an_empty_property : Specification
{
    const string Left = "";
    const string Right = "something";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left) + Right;

    [Fact] void should_hold_only_property_added_on() => _result.Path.ShouldEqual(Right);
}
