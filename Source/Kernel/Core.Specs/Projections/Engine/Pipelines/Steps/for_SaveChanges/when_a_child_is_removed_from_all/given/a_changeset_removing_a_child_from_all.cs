// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges.when_a_child_is_removed_from_all.given;

public class a_changeset_removing_a_child_from_all : for_SaveChanges.given.a_save_changes_step
{
    protected ISink _writingSink;
    protected ChildRemovedFromAll _removal;
    protected Key _firstParent;
    protected Key _secondParent;
    protected readonly List<string> _operations = [];

    void Establish()
    {
        _removal = new ChildRemovedFromAll(new PropertyPath("groups"), new PropertyPath("groupId"), "group-1", ArrayIndexers.NoIndexers);
        _firstParent = new Key("user-1", ArrayIndexers.NoIndexers);
        _secondParent = new Key("user-2", ArrayIndexers.NoIndexers);
        _context.Changeset.Changes.Returns([_removal]);

        _writingSink = Substitute.For<ISink>();
        _writingSink.FindKeysHoldingChild(_removal).Returns(_ =>
        {
            _operations.Add("lookup");
            return Task.FromResult<IReadOnlyCollection<Key>>([_firstParent, _secondParent]);
        });
        _writingSink.ApplyChanges(_context.Key, _context.Changeset, _context.EventSequenceNumber, Arg.Any<SinkWriteMode>()).Returns(_ =>
        {
            _operations.Add("write");
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
    }
}
