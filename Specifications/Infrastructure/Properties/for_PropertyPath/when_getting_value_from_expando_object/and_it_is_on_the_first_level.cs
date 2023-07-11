// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Dynamic;

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class and_it_is_on_the_first_level : Specification
{
    ExpandoObject input;
    PropertyPath property_path;
    object result;

    void Establish()
    {
        input = new { property = 42 }.AsExpandoObject();
        property_path = new("property");
    }

    void Because() => result = property_path.GetValue(input, ArrayIndexers.NoIndexers);

    [Fact] void should_return_value() => result.ShouldEqual(42);
}
