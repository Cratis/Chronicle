// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_getting_property_info_for_a_deep_nested_value : Specification
{
    record Context(DateTimeOffset Occurred);
    PropertyInfo result;

    void Because() => result = new PropertyPath("occurred.year").GetPropertyInfoFor<Context>();

    [Fact] void should_return_property_info_for_last_segment() => result.Name.ShouldEqual("Year");
}
