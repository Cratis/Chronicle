// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeSetCache;

public class when_discovering_concurrently : Specification
{
    readonly EventTypeSetCache<object> _cache = new();
    readonly object[] _results = new object[32];
    int _builds;

    void Because() => Parallel.For(0, _results.Length, index =>
        _results[index] = _cache.Get(index % 2 == 0 ? [typeof(string), typeof(int)] : [typeof(int), typeof(string)], typeof(string), typeof(int), _ =>
        {
            Interlocked.Increment(ref _builds);
            return new object();
        }));

    [Fact] void should_build_only_once() => _builds.ShouldEqual(1);
    [Fact] void should_share_the_result() => _results.All(result => ReferenceEquals(result, _results[0])).ShouldBeTrue();
}
