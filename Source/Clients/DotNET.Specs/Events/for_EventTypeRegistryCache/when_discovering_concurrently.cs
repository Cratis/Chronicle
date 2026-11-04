// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache;

public class when_discovering_concurrently : Specification
{
    readonly EventTypeRegistryCache<object> _cache = new();
    readonly object[] _results = new object[32];
    IEventTypes _registry;
    int _builds;

    void Establish()
    {
        _registry = Substitute.For<IEventTypes>();
        _registry.AllClrTypes.Returns(new[] { typeof(string), typeof(int) }.ToImmutableList());
    }

    void Because() => Parallel.For(0, _results.Length, index =>
        _results[index] = _cache.Get(_registry, typeof(string), typeof(int), _ =>
        {
            Interlocked.Increment(ref _builds);
            return new object();
        }));

    [Fact] void should_build_only_once() => _builds.ShouldEqual(1);
    [Fact] void should_share_the_result() => _results.All(result => ReferenceEquals(result, _results[0])).ShouldBeTrue();
}
