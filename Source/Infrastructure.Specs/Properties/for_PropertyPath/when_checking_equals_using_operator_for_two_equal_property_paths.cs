// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_checking_equals_using_operator_for_two_equal_property_paths : Specification
{
    const string Path = "some.[path]";

    PropertyPath _left = new(Path);
    PropertyPath _right = new(Path);

    bool _result;

    void Because() => _result = _left == _right;

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
}
