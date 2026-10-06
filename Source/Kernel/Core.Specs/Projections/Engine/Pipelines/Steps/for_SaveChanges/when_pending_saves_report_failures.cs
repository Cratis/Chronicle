// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_pending_saves_report_failures : given.a_save_changes_step
{
    readonly FailedPartition[] _firstFailures = [new("first", 1UL), new("second", 2UL)];
    readonly FailedPartition[] _secondFailures = [new("third", 3UL)];
    SaveChanges _step;

    void Establish()
    {
        _context.Changeset.HasChanges.Returns(false);
        var sink = Substitute.For<ISink>();
        var firstKey = new Key("pending-first", ArrayIndexers.NoIndexers);
        var secondKey = new Key("pending-second", ArrayIndexers.NoIndexers);
        _context.AddPendingFutureSave(firstKey, Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>());
        _context.AddPendingFutureSave(secondKey, Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>());
        sink.ApplyChanges(firstKey, Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>()).Returns(_firstFailures);
        sink.ApplyChanges(secondKey, Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>()).Returns(_secondFailures);
        _step = new(sink, Substitute.For<IChangesetStorage>(), true, NullLogger<SaveChanges>.Instance);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_collect_every_pending_save_failure() => _context.FailedPartitions.ShouldContainOnly(_firstFailures.Concat(_secondFailures));
}
