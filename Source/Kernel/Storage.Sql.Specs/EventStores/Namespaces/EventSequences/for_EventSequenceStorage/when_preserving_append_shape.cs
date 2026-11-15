// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_preserving_append_shape : Specification
{
    static readonly string[] _columns = ["SequenceNumber", "EventSourceType", "EventSourceId", "EventSource", "EventStreamType", "EventStreamId", "CompensationFor", "Compensations", "PublicationIdentityHash", "PublicationId", "PublicationFingerprint", "Type", "AppendedGeneration", "Content", "Occurred", "CorrelationId", "Causation", "CausedBy", "Tags", "Subject", "ContentHashes", "RevisedGeneration", "DerivedGenerations"];
    given.EventSequenceStorageHost _host;
    given.RecordingCommands _recorder;
    IEventSequenceStorage _storage;
    string[] _single;
    string[] _batch;
    string[] _at;
    string[] _range;
    string[] _terminal;

    void Establish()
    {
        _recorder = new();
        _host = given.EventSequenceStorageHost.ForSqlite(Substitute.For<IIdentityStorage>(), Substitute.For<IEventTypesStorage>(), _recorder);
        _storage = _host.Open();
        _recorder.Commands.Clear();
    }

    async Task Because()
    {
        (await _storage.Append(0, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("shape", 1), CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new() }, new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = EventHash.NotSet })).IsSuccess.ShouldBeTrue();
        _single = TakeCommands();
        (await _storage.AppendMany(Enumerable.Range(1, 100).Select(number => new EventToAppendToStorage((ulong)number, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("shape", 1), CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new(), EventHash.NotSet)))).IsSuccess.ShouldBeTrue();
        _batch = TakeCommands();
        await _storage.GetEventAt(0);
        _at = TakeCommands();
        using var cursor = await _storage.GetRange(0, 99);
        (await cursor.MoveNext()).ShouldBeTrue();
        cursor.Current.Count().ShouldEqual(100);
        _range = TakeCommands();
        (await cursor.MoveNext()).ShouldBeFalse();
        _terminal = TakeCommands();
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_keep_single_append_statement_count() => _single.Length.ShouldEqual(2);
    [Fact] void should_keep_batch_append_statement_count() => _batch.Length.ShouldEqual(200);
    [Fact] void should_insert_once_for_single_append() => Inserts(_single).Count().ShouldEqual(1);
    [Fact] void should_insert_once_per_batch_event() => Inserts(_batch).Count().ShouldEqual(100);
    [Fact] void should_keep_single_insert_columns() => AssertInsertColumns(_single);
    [Fact] void should_keep_batch_insert_columns() => AssertInsertColumns(_batch);
    [Fact] void should_select_one_event_for_point_read() => EventSelects(_at).Count().ShouldEqual(1);
    [Fact] void should_select_one_event_batch() => EventSelects(_range).Count().ShouldEqual(1);
    [Fact] void should_load_named_tags_once_for_point_read() => NamedTagSelects(_at).Count().ShouldEqual(1);
    [Fact] void should_load_named_tags_once_for_batch_read() => NamedTagSelects(_range).Count().ShouldEqual(1);
    [Fact] void should_keep_point_read_statement_count() => _at.Length.ShouldEqual(2);
    [Fact] void should_keep_batch_read_statement_count() => _range.Length.ShouldEqual(2);
    [Fact] void should_keep_terminal_probe_statement_count() => _terminal.Length.ShouldEqual(1);
    [Fact] void should_probe_only_the_event_table_at_the_end() => EventSelects(_terminal).Count().ShouldEqual(1);
    [Fact] void should_keep_cursor_read_columns() => AssertSelectColumns(_range);

    string[] TakeCommands()
    {
        var commands = _recorder.Commands.ToArray();
        _recorder.Commands.Clear();
        return commands;
    }

    static IEnumerable<string> Inserts(IEnumerable<string> commands) => commands.Where(command => command.StartsWith("INSERT INTO", StringComparison.Ordinal));
    static IEnumerable<string> EventSelects(IEnumerable<string> commands) => commands.Where(command => command.StartsWith("SELECT", StringComparison.Ordinal) && !command.Contains("cratis_named_tags", StringComparison.OrdinalIgnoreCase));
    static IEnumerable<string> NamedTagSelects(IEnumerable<string> commands) => commands.Where(command => command.StartsWith("SELECT", StringComparison.Ordinal) && command.Contains("cratis_named_tags", StringComparison.OrdinalIgnoreCase));

    static void AssertInsertColumns(IEnumerable<string> commands)
    {
        foreach (var command in Inserts(commands))
        {
            var start = command.IndexOf('(') + 1;
            var end = command.IndexOf(')');
            var columns = command[start..end].Split(',').Select(column => column.Trim().Trim('"')).Order().ToArray();
            columns.ShouldEqual(_columns.Order().ToArray());
        }
    }

    static void AssertSelectColumns(IEnumerable<string> commands)
    {
        foreach (var command in EventSelects(commands))
        {
            var projection = command[..command.IndexOf("FROM", StringComparison.Ordinal)];
            var columns = projection["SELECT ".Length..].Split(',').Select(column => column.Trim().Split('.')[1].Trim('"')).Order().ToArray();
            columns.ShouldEqual(_columns.Order().ToArray());
        }
    }
}
