// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_there_are_no_changes_or_pending_futures : given.a_save_changes_step
{
    ProjectionEventContext _result;

    void Establish() => _context.Changeset.HasChanges.Returns(false);

    async Task Because() => _result = await CreateStep(false).Perform(_projection, _context);

    [Fact] void should_return_the_unchanged_context() => _result.ShouldEqual(_context);
    [Fact] void should_not_write_to_the_sink() => _sink.WriteModes.ShouldBeEmpty();
}
