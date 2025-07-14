// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_to_an_empty_property_with_a_dictionary_type : Specification
{
    const string Left = "";
    const string Right = "something";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left).AddProperty(Right, typeof(Dictionary<string, string>));

    [Fact] void should_hold_only_property_added_on() => _result.Path.ShouldEqual(Right);
}
