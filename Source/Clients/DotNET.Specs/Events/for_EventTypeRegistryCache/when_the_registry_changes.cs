// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache;

public class when_the_registry_changes : Specification
{
    EventTypeRegistryCache<Type[]> _cache;
    IEventTypes _registry;
    object _context;
    Type[] _first;
    Type[] _second;
    Type[] _secondWithoutContext;
    int _buildCount;

    void Establish()
    {
        _cache = new();
        _registry = Substitute.For<IEventTypes>();
        _registry.AllClrTypes.Returns(new[] { typeof(First) }.ToImmutableList());
        _context = new();
    }

    void Because()
    {
        _first = _cache.Get(_registry, GetType(), _context, Build);
        _cache.Get(_registry, GetType(), null, Build);
        _registry.AllClrTypes.Returns(new[] { typeof(Second) }.ToImmutableList());
        _second = _cache.Get(_registry, GetType(), _context, Build);
        _secondWithoutContext = _cache.Get(_registry, GetType(), null, Build);
    }

    [Fact] void should_build_both_contexts_again() => _buildCount.ShouldEqual(4);
    [Fact] void should_keep_existing_invokers_on_their_original_snapshot() => _first.ShouldContainOnly(typeof(First));
    [Fact] void should_refresh_the_context_cache() => _second.ShouldContainOnly(typeof(Second));
    [Fact] void should_refresh_the_default_cache() => _secondWithoutContext.ShouldContainOnly(typeof(Second));

    Type[] Build(IEnumerable<Type> types)
    {
        _buildCount++;
        return types.ToArray();
    }

    class First;
    class Second;
}
