// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_an_empty_property_to_an_existing_property : Specification
{
    const string left = "left";
    const string right = "";

    PropertyPath result;

    void Because() => result = new PropertyPath(left) + right;

    [Fact] void should_combine_with_dot_separator() => result.Path.ShouldEqual(left);
}
