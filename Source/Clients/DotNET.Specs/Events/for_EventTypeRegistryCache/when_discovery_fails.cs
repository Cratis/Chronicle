// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache;

public class when_discovery_fails : Specification
{
    readonly EventTypeRegistryCache<object> _cache = new();
    readonly Exception _failure = new("discovery failed");
    IEventTypes _registry;
    Exception _error;
    object _result;
    object _expected;

    void Establish()
    {
        _registry = Substitute.For<IEventTypes>();
        _registry.AllClrTypes.Returns(new[] { typeof(string) }.ToImmutableList());
    }

    void Because()
    {
        _error = Catch.Exception(() => _cache.Get(_registry, typeof(string), typeof(int), _ => throw _failure));
        _expected = new object();
        _result = _cache.Get(_registry, typeof(string), typeof(int), _ => _expected);
    }

    [Fact] void should_preserve_the_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_retry_discovery() => _result.ShouldBeSame(_expected);
}
