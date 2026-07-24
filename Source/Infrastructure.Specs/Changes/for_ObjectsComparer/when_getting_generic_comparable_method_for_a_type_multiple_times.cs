// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_getting_generic_comparable_method_for_a_type_multiple_times : given.an_object_comparer
{
    MethodInfo _first;
    MethodInfo _second;

    void Because()
    {
        _first = comparer.GetGenericCompareToFor(typeof(MyGenericComparable));
        _second = comparer.GetGenericCompareToFor(typeof(MyGenericComparable));
    }

    [Fact] void should_return_the_same_cached_method_instance() => ReferenceEquals(_second, _first).ShouldBeTrue();
    [Fact] void should_resolve_the_compare_to_method() => _first.Name.ShouldEqual(nameof(IComparable<object>.CompareTo));
}
