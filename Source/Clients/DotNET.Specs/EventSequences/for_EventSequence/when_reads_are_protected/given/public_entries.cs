// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reads_are_protected.given;

public class public_entries : when_committing_with_read_protection.given.a_trusted_commit
{
    protected Dictionary<string, Exception> _errors;

    protected async Task ExerciseAppends(ReadAllowance? allowance)
    {
        using var frame = _unit.EnforceReadProtection();
        using var region = allowance is null ? null : _unit.AllowReads(allowance.Value, "append guard matrix");
        await Capture(new Dictionary<string, Func<Task>>
        {
            ["prepared"] = () => _sequence.AppendPrepared("source", null!),
            ["prepared routed"] = () => _sequence.AppendPreparedThroughEventSource(typeof(object), "source", null!),
            ["single"] = () => _sequence.Append("source", new object()),
            ["routed"] = () => _sequence.AppendThroughEventSource(typeof(object), "source", new object()),
            ["routed batch"] = () => _sequence.AppendManyThroughEventSource(typeof(object), "source", Poison<object>()),
            ["named single"] = () => _sequence.AppendWithNamedTags("source", new object(), Poison<NamedTag>()),
            ["batch"] = () => _sequence.AppendMany("source", Poison<object>()),
            ["named batch"] = () => _sequence.AppendManyWithNamedTags("source", Poison<object>(), Poison<NamedTag>()),
            ["labelled batch"] = () => _sequence.AppendMany(Poison<EventForEventSourceId>()),
            ["named labelled batch"] = () => _sequence.AppendManyWithNamedTags(Poison<EventForEventSourceId>(), Poison<NamedTag>()),
            ["complete stream"] = () => _sequence.CompleteStream("stream", "id"),
            ["complete scope"] = () => _sequence.CompleteStream(ClosedStreamScope.ForEventSource("source"))
        });
    }

    protected async Task ExerciseReads()
    {
        using var frame = _unit.EnforceReadProtection();
        await Capture(new Dictionary<string, Func<Task>>
        {
            ["verify"] = () => _sequence.VerifyContent(EventSequenceNumber.First, null!),
            ["has events"] = () => _sequence.HasEventsFor("other-key"),
            ["metadata"] = () => _sequence.GetMetadataAt(1000),
            ["metadata batch"] = () => _sequence.GetMetadataAt(Poison<EventSequenceNumber>()),
            ["from position"] = () => _sequence.GetFromSequenceNumber(EventSequenceNumber.First),
            ["filtered"] = () => _sequence.GetForEventSourceIdAndEventTypes("other-key", Poison<EventType>()),
            ["next"] = _sequence.GetNextSequenceNumber,
            ["tail"] = () => _sequence.GetTailSequenceNumber(),
            ["observer tail"] = () => _sequence.GetTailSequenceNumberForObserver(typeof(object)),
            ["closed streams"] = () => _sequence.GetClosedStreams(),
            ["completed stream"] = () => _sequence.IsStreamCompleted("stream", "id"),
            ["completed scope"] = () => _sequence.IsStreamCompleted(ClosedStreamScope.ForEventSource("source"))
        });
    }

    static IEnumerable<T> Poison<T>() => Enumerable.Repeat(default(T)!, 1).Select<T, T>(_ => throw new Exception("Public guard must run before enumeration"));

    async Task Capture(Dictionary<string, Func<Task>> operations)
    {
        _sequences.ClearReceivedCalls();
        _eventSerializer.ClearReceivedCalls();
        _concurrencyScopeStrategies.ClearReceivedCalls();
        _errors = [];
        foreach (var (name, operation) in operations)
        {
            _errors.Add(name, await Catch.Exception(operation));
        }
    }
}
