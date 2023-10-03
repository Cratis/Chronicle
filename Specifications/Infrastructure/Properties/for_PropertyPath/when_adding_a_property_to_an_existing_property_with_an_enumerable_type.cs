// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_adding_a_property_to_an_existing_property_with_an_enumerable_type : Specification
{
    const string left = "left";
    const string right = "right";

    PropertyPath result;

    void Because() => result = new PropertyPath(left).AddProperty(right, typeof(List<string>));

    [Fact] void should_combine_with_dot_separator() => result.Path.ShouldEqual($"{left}.[{right}]");
}
