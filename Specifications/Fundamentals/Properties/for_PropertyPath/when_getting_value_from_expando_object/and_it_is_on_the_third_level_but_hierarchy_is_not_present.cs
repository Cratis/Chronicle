// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class and_it_is_on_the_third_level_but_hierarchy_is_not_present : Specification
{
    ExpandoObject input;
    PropertyPath property_path;
    object result;

    void Establish()
    {
        input = new ExpandoObject();
        property_path = new("first_level.second_level.third_level");
    }

    void Because() => result = property_path.GetValue(input, ArrayIndexers.NoIndexers);

    [Fact] void should_return_null() => result.ShouldBeNull();
}
