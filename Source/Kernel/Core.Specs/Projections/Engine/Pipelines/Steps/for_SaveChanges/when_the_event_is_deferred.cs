// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_event_is_deferred : given.a_save_changes_step
{
    ProjectionEventContext _result;

    void Establish() => _context.AddDeferredFuture(new ProjectionFuture(
        Guid.NewGuid(), "projection", _context.Event, "parents", "parents.children", "id", "id", _context.Key, DateTimeOffset.UnixEpoch));

    async Task Because() => _result = await CreateStep(false).Perform(_projection, _context);

    [Fact] void should_return_the_unchanged_context() => _result.ShouldEqual(_context);
    [Fact] void should_not_write_to_the_sink() => _sink.WriteModes.ShouldBeEmpty();
}
