// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Objects.for_ObjectExtensions;

public class when_comparing_two_expando_objects_that_have_the_same_content : Specification
{
    ExpandoObject _left;
    ExpandoObject _right;
    bool _result;

    void Establish()
    {
        _left = new();
        ((dynamic)_left).Something = "Hello";
        ((dynamic)_left).Another = "World";

        _right = new();
        ((dynamic)_right).Something = "Hello";
        ((dynamic)_right).Another = "World";
    }

    void Because() => _result = _left.IsEqualTo(_right);

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
}
