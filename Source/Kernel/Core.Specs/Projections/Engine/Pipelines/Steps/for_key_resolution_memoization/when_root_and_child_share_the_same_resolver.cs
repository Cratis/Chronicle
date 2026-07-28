// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_key_resolution_memoization;

public class when_root_and_child_share_the_same_resolver : given.a_resolve_key_and_handle_event_step
{
    void Establish()
    {
        _projection.GetKeyResolverFor(_eventType).Returns(_sharedResolver);
        _child.HasKeyResolverFor(_eventType).Returns(true);
        _child.GetKeyResolverFor(_eventType).Returns(_sharedResolver);
    }

    async Task Because()
    {
        _context = await _resolveKey.Perform(_projection, _context);
        _context = await _handleEvent.Perform(_projection, _context);
    }

    [Fact] void should_resolve_the_shared_key_only_once() => _sharedResolverCallCount.ShouldEqual(1);
}
