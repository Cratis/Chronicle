// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache;

public class when_inputs_differ : Specification
{
    readonly EventTypeRegistryCache<object> _cache = new();
    IEventTypes _registry;
    IEventTypes _otherRegistry;
    object _original;
    object _again;
    object _otherRegistryResult;
    object _artifact;
    object _context;

    void Establish()
    {
        _registry = Registry(typeof(string));
        _otherRegistry = Registry(typeof(string));
    }

    void Because()
    {
        _original = Get(_registry, typeof(string), typeof(int));
        _again = Get(_registry, typeof(string), typeof(int));
        _otherRegistryResult = Get(_otherRegistry, typeof(string), typeof(int));
        _artifact = Get(_registry, typeof(int), typeof(int));
        _context = Get(_registry, typeof(string), typeof(string));
    }

    object Get(IEventTypes registry, Type artifact, object context) => _cache.Get(registry, artifact, context, _ => new object());

    static IEventTypes Registry(params Type[] types)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(types.ToImmutableList());
        return registry;
    }

    [Fact] void should_reuse_the_result_within_a_registry() => _again.ShouldBeSame(_original);
    [Fact] void should_separate_registries() => _otherRegistryResult.ShouldNotBeSame(_original);
    [Fact] void should_separate_artifacts() => _artifact.ShouldNotBeSame(_original);
    [Fact] void should_separate_contexts() => _context.ShouldNotBeSame(_original);
}
