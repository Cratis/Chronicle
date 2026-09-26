// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath.when_getting_a_value_for_a_path_that_does_not_exist;

public class and_the_target_is_a_regular_object : Specification
{
    PropertyPath _path;
    TheRoot _target;
    object _value;
    bool _hasValue;

    void Establish()
    {
        _path = new("child.name");
        _target = new();
    }

    void Because()
    {
        _value = _path.GetValue(_target, ArrayIndexers.NoIndexers);
        _hasValue = _path.HasValue(_target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_not_have_a_value() => _value.ShouldBeNull();
    [Fact] void should_report_it_has_no_value() => _hasValue.ShouldBeFalse();
    [Fact] void should_not_instantiate_the_missing_child() => _target.Child.ShouldBeNull();

    public class TheRoot
    {
        public TheChild Child { get; set; }
    }

    public class TheChild
    {
        public string Name { get; set; }
    }
}
