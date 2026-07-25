// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_key_resolution_memoization;

public class when_root_and_child_use_distinct_resolvers : given.a_resolve_key_and_handle_event_step
{
    int _childResolverCallCount;

    void Establish()
    {
        var childKey = new Key("child-key", ArrayIndexers.NoIndexers);
        Task<KeyResolverResult> ChildResolver(Storage.EventSequences.IEventSequenceStorage storage, Storage.Sinks.ISink sink, Concepts.Events.AppendedEvent @event)
        {
            _childResolverCallCount++;
            return Task.FromResult(KeyResolverResult.Resolved(childKey));
        }

        _projection.GetKeyResolverFor(_eventType).Returns(_sharedResolver);
        _child.HasKeyResolverFor(_eventType).Returns(true);
        _child.GetKeyResolverFor(_eventType).Returns(ChildResolver);
    }

    async Task Because()
    {
        _context = await _resolveKey.Perform(_projection, _context);
        _context = await _handleEvent.Perform(_projection, _context);
    }

    [Fact] void should_resolve_the_root_key_once() => _sharedResolverCallCount.ShouldEqual(1);
    [Fact] void should_resolve_the_distinct_child_key_once() => _childResolverCallCount.ShouldEqual(1);
}
