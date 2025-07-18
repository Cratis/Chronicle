// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class and_it_is_on_the_first_level : Specification
{
    ExpandoObject _input;
    PropertyPath _propertyPath;
    object _result;

    void Establish()
    {
        _input = new { property = 42 }.AsExpandoObject();
        _propertyPath = new("property");
    }

    void Because() => _result = _propertyPath.GetValue(_input, ArrayIndexers.NoIndexers);

    [Fact] void should_return_value() => _result.ShouldEqual(42);
}
