// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_segment_using_operator_to_an_existing_property : Specification
{
    const string Left = "left";
    const string Right = "right";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left) + new PropertyName(Right);

    [Fact] void should_combine_with_dot_separator() => _result.Path.ShouldEqual($"{Left}.{Right}");
}
