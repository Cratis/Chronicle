// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Properties.for_PropertyPath.when_getting_a_value_for_a_path_that_does_not_exist;

/// <summary>
/// A read must not write. Both accessors resolved the path through EnsurePath, which materializes every
/// missing intermediate and assigns it back into the object being read - so asking whether a value was
/// there created it, and the event content handed to a projection grew properties nobody appended (#4128).
/// </summary>
public class and_the_target_is_an_expando_object : Specification
{
    PropertyPath _path;
    ExpandoObject _target;
    object _value;
    bool _hasValue;

    void Establish()
    {
        _path = new("someProperty.someChild.someGrandChild");
        _target = new();
    }

    void Because()
    {
        _value = _path.GetValue(_target, ArrayIndexers.NoIndexers);
        _hasValue = _path.HasValue(_target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_not_have_a_value() => _value.ShouldBeNull();
    [Fact] void should_report_it_has_no_value() => _hasValue.ShouldBeFalse();
    [Fact] void should_leave_the_target_untouched() => _target.Any().ShouldBeFalse();
}
