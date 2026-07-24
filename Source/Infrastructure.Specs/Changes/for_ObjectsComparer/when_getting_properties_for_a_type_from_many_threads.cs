// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_getting_properties_for_a_type_from_many_threads : given.an_object_comparer
{
    record SomeType(string StringValue, int IntValue, double DoubleValue);

    ConcurrentBag<PropertyInfo[]> _results;

    void Because()
    {
        _results = [];
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            _results.Add(comparer.GetPropertiesFor(typeof(SomeType))));
    }

    [Fact] void should_return_a_result_for_every_thread() => _results.Count.ShouldEqual(1000);
    [Fact] void should_always_return_the_same_cached_array_instance() => _results.Distinct().Count().ShouldEqual(1);
}
