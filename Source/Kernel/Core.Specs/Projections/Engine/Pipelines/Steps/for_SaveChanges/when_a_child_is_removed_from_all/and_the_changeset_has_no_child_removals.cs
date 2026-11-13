// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.Changes;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges.when_a_child_is_removed_from_all;

public class and_the_changeset_has_no_child_removals : given.a_changeset_removing_a_child_from_all
{
    async Task Because()
    {
        _context.Changeset.Changes.Returns([]);
        var gate = new ProjectionWatchGate(TimeProvider.System);
        gate.Open(TimeSpan.FromMinutes(1));
        await new SaveChanges(_writingSink, Substitute.For<IChangesetStorage>(), true, Substitute.For<ILogger<SaveChanges>>(), watchGate: gate).Perform(_projection, _context);
    }

    [Fact] void should_not_look_up_the_parents() => _writingSink.DidNotReceive().FindKeysHoldingChild(Arg.Any<ChildRemovedFromAll>());
    [Fact] void should_not_record_any_affected_parents() => _context.AffectedParentKeys.ShouldBeEmpty();
}
