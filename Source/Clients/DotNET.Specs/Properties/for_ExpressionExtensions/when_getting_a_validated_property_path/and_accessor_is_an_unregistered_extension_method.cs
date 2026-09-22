// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path.given;

namespace Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path;

public class and_accessor_is_an_unregistered_extension_method : given.an_event
{
    bool _result;
    PropertyPath _path = PropertyPath.NotSet;

    void Because()
    {
        // Same shape as a registered derived function - a zero-argument extension method call on a member
        // chain rooted at the parameter - but not registered in DerivedPropertyFunctions, so it must still
        // be rejected the same way an arbitrary method call always has been.
        Expression<Func<SomeEvent, object>> accessor = e => e.Occurred.NotADerivedFunction();
        _result = accessor.TryGetPropertyPath(out _path);
    }

    [Fact] void should_not_succeed() => _result.ShouldBeFalse();
    [Fact] void should_not_extract_a_property_path() => _path.IsSet.ShouldBeFalse();
}
