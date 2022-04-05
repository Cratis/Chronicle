// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_checking_equals_using_operator_for_two_equal_property_paths : Specification
{
    const string path = "some.[path]";

    PropertyPath left = new(path);
    PropertyPath right = new(path);

    bool result;

    void Because() => result = left == right;

    [Fact] void should_be_equal() => result.ShouldBeTrue();
}
