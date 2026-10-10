// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_a_pending_future_write_fails : given.a_save_changes_step
{
    ISink _failingSink;
    FailedPartition _failure;
    ProjectionEventContext _result;

    void Establish()
    {
        _context.Changeset.HasChanges.Returns(false);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        var key = new Key("future-key", ArrayIndexers.NoIndexers);
        _context.AddPendingFutureSave(key, changeset);
        _failure = new FailedPartition(key, _context.EventSequenceNumber);
        _failingSink = Substitute.For<ISink>();
        _failingSink.ApplyChanges(key, changeset, _context.EventSequenceNumber).Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failure]));
    }

    async Task Because() => _result = await new SaveChanges(_failingSink, Substitute.For<IChangesetStorage>(), true, Substitute.For<ILogger<SaveChanges>>()).Perform(_projection, _context);

    [Fact] void should_report_the_failed_partition() => _result.FailedPartitions.ShouldContainOnly(_failure);
}
