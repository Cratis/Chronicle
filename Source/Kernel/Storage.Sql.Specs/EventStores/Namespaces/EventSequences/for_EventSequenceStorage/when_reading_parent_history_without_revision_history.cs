// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_reading_parent_history_without_revision_history : Specification
{
    given.EventSequenceStorageHost _host;
    given.RecordingCommands _recorder;
    IEventSequenceStorage _storage;
    string[] _commands;

    async Task Establish()
    {
        _recorder = new();
        _host = given.EventSequenceStorageHost.ForSqlite(Substitute.For<IIdentityStorage>(), Substitute.For<IEventTypesStorage>(), _recorder);
        _storage = _host.Open();
        (await _storage.AppendMany(Enumerable.Range(0, 4).Select(number => new EventToAppendToStorage((ulong)number, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("shape", 1), CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new(), EventHash.NotSet)))).IsSuccess.ShouldBeTrue();
        _recorder.Commands.Clear();
    }

    async Task Because()
    {
        var result = await _storage.GetInstancesOfAnyInRange(["source"], [new EventType("shape", 1).Id], 2, 3);
        result["source"].Count.ShouldEqual(3);
        _commands = _recorder.Commands.ToArray();
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_not_select_the_original_content_column() => EventSelects().Any(command => command.Contains("OriginalContent", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_select_the_revisions_column() => EventSelects().Any(command => command.Contains("\"Revisions\"", StringComparison.Ordinal)).ShouldBeFalse();

    IEnumerable<string> EventSelects() => _commands.Where(command => command.StartsWith("SELECT", StringComparison.Ordinal) && !command.Contains("cratis_named_tags", StringComparison.OrdinalIgnoreCase));
}
