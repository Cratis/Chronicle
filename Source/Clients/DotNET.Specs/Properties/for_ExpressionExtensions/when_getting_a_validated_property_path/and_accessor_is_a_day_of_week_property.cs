// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path;

public class and_accessor_is_a_day_of_week_property : given.an_event
{
    bool _result;
    PropertyPath _path = PropertyPath.NotSet;

    void Because()
    {
        // DayOfWeek is a real property on DateTimeOffset, so it already works as a plain member-access
        // chain - no derived function support is needed for this one, unlike ISOWeek().
        Expression<Func<SomeEvent, object>> accessor = e => e.Occurred.DayOfWeek;
        _result = accessor.TryGetPropertyPath(out _path);
    }

    [Fact] void should_succeed() => _result.ShouldBeTrue();
    [Fact] void should_extract_the_full_property_path() => _path.Path.ShouldEqual("Occurred.DayOfWeek");
}
