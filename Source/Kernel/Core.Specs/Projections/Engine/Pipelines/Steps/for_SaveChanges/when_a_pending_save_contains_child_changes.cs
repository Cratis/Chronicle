// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_a_pending_save_contains_child_changes : given.a_pending_future_save
{
    protected override bool FoldIntoMainWrite => true;

    void Establish()
    {
        _context.Changeset.HasChanges.Returns(true);
        _futureChangeset.Changes.Returns([new ChildAdded(new ExpandoObject(), "children", "id", "child", ArrayIndexers.NoIndexers)]);
        _writingSink.ApplyChanges(_context.Key, _context.Changeset, _context.EventSequenceNumber, SinkWriteMode.OnlyWhenAdvancingWatermark).Returns(_ =>
        {
            _operations.Add("main-write");
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_write_the_child_changes_after_the_main_changes() => _operations.ShouldEqual(["main-write", "write", "resolve"]);
    [Fact] void should_not_fold_the_child_changes_into_the_main_changeset() => _context.Changeset.DidNotReceive().Add(Arg.Any<Change>());
}
