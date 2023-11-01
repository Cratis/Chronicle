// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Objects.for_ObjectExtensions;

public class when_comparing_two_expando_objects_that_with_different_content : Specification
{
    ExpandoObject left;
    ExpandoObject right;
    bool result;

    void Establish()
    {
        left = new();
        ((dynamic)left).Something = "World";
        ((dynamic)left).Another = "Hello";

        right = new();
        ((dynamic)right).Something = "Hello";
        ((dynamic)right).Another = "World";
    }

    void Because() => result = left.IsEqualTo(right);

    [Fact] void should_not_be_equal() => result.ShouldBeFalse();
}
