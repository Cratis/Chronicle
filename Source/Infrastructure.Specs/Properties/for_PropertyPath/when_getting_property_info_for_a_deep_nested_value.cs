// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_getting_property_info_for_a_deep_nested_value : Specification
{
    record Context(DateTimeOffset Occurred);
    PropertyInfo _result;

    void Because() => _result = new PropertyPath("occurred.year").GetPropertyInfoFor<Context>();

    [Fact] void should_return_property_info_for_last_segment() => _result.Name.ShouldEqual("Year");
}
