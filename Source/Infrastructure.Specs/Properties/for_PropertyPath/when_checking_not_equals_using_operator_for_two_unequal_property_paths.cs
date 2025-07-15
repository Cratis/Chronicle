// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_checking_not_equals_using_operator_for_two_unequal_property_paths : Specification
{
    PropertyPath _left = new("some.[path]");
    PropertyPath _right = new("some.other.[path]");

    bool _result;

    void Because() => _result = _left != _right;

    [Fact] void should_be_true() => _result.ShouldBeTrue();
}
