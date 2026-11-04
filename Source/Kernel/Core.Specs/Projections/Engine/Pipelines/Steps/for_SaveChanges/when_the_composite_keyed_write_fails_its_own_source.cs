// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_composite_keyed_write_fails_its_own_source : given.a_save_changes_step
{
    readonly FailedPartition _failure = new((Key)"A", 5UL);
    Key _compositeKey;
    IChangesetStorage _changesetStorage;
    SaveChanges _step;
    ProjectionEventContext _result;

    void Establish()
    {
        var value = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)value;
        dictionary["Tenant"] = "tenant-1";
        dictionary["Account"] = "account-1";
        _compositeKey = new Key(value, ArrayIndexers.NoIndexers);

        _changesetStorage = Substitute.For<IChangesetStorage>();
        var incoming = AppendedEvent.EmptyWithEventType(new EventType("TestEvent", EventTypeGeneration.First));
        _context.Changeset.Incoming.Returns(incoming with { Context = incoming.Context with { EventSourceId = "A", SequenceNumber = 5UL } });
        _context = _context with { Key = _compositeKey };
        _sink.FailedPartitions = [_failure];
        _step = new SaveChanges(_sink, _changesetStorage, false, Substitute.For<ILogger<SaveChanges>>());
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] async Task should_not_save_the_changeset_audit() => await _changesetStorage.DidNotReceive().Save(Arg.Any<ReadModelContainerName>(), Arg.Any<Key>(), Arg.Any<EventType>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CorrelationId>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>());
    [Fact] void should_report_the_own_sources_failure() => _result.FailedPartitions.ShouldContain(_failure);
}
