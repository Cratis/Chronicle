// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_whose_name_is_the_this_accessor : Specification
{
    const string Left = "left";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left).AddProperty(PropertyPath.ThisAccessorValue);

    [Fact] void should_combine_with_dot_separator() => _result.Path.ShouldEqual($"{Left}.{PropertyPath.ThisAccessorValue}");
    [Fact] void should_have_last_segment_be_this_accessor() => _result.LastSegment.ShouldBeOfExactType<ThisAccessor>();
}
