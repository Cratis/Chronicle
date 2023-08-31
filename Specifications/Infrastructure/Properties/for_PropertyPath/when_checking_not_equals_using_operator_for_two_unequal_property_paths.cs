// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_checking_not_equals_using_operator_for_two_unequal_property_paths : Specification
{
    PropertyPath left = new("some.[path]");
    PropertyPath right = new("some.other.[path]");

    bool result;

    void Because() => result = left != right;

    [Fact] void should_be_true() => result.ShouldBeTrue();
}
