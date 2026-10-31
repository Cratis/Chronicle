// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeSetCache;

public class when_discovery_fails : Specification
{
    readonly EventTypeSetCache<object> _cache = new();
    readonly Exception _failure = new("discovery failed");
    Exception _error;
    object _result;
    object _expected;

    void Because()
    {
        _error = Catch.Exception(() => _cache.Get([typeof(string)], typeof(string), typeof(int), _ => throw _failure));
        _expected = new object();
        _result = _cache.Get([typeof(string)], typeof(string), typeof(int), _ => _expected);
    }

    [Fact] void should_preserve_the_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_retry_discovery() => _result.ShouldBeSame(_expected);
}
