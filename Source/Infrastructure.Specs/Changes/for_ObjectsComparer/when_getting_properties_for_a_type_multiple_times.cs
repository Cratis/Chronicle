// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_getting_properties_for_a_type_multiple_times : given.an_object_comparer
{
    record SomeType(string StringValue, int IntValue);

    PropertyInfo[] _first;
    PropertyInfo[] _second;

    void Because()
    {
        _first = comparer.GetPropertiesFor(typeof(SomeType));
        _second = comparer.GetPropertiesFor(typeof(SomeType));
    }

    [Fact] void should_return_the_same_cached_array_instance() => ReferenceEquals(_second, _first).ShouldBeTrue();
    [Fact] void should_return_all_public_properties() => _first.Length.ShouldEqual(typeof(SomeType).GetProperties().Length);
}
