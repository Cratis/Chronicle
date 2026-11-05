// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Changes;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges.when_a_child_is_removed_from_all;

public class and_the_projection_is_watched : given.a_changeset_removing_a_child_from_all
{
    async Task Because()
    {
        var gate = new ProjectionWatchGate(TimeProvider.System);
        gate.Open(TimeSpan.FromMinutes(1));
        await new SaveChanges(_writingSink, Substitute.For<IChangesetStorage>(), true, Substitute.For<ILogger<SaveChanges>>(), watchGate: gate).Perform(_projection, _context);
    }

    [Fact] void should_look_up_the_parents_before_writing() => _operations.ShouldEqual(["lookup", "write"]);
    [Fact] void should_record_the_affected_parents() => _context.AffectedParentKeys.ShouldContainOnly([_firstParent, _secondParent]);
}
