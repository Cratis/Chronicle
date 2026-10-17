// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused.given;

public class public_entries : a_unit_with_native_sequence
{
    protected Dictionary<string, Exception> _errors;
    protected int _notifications;

    protected async Task ExerciseAppends(ReadAllowance? allowance = null)
    {
        using var frame = Refuse();
        using var protection = allowance is null ? null : _unit.EnforceReadProtection();
        using var region = allowance is null ? null : _unit.AllowReads(allowance.Value, "append refusal matrix");
        using var subscription = _sequence.AppendOperations.Subscribe(_ => _notifications++);
        ClearCalls();
        _errors = [];
        foreach (var (name, operation) in new Dictionary<string, Func<Task>>
        {
            ["single"] = () => _sequence.Append("source", new object()),
            ["named single"] = () => _sequence.AppendWithNamedTags("source", new object(), Poison<NamedTag>()),
            ["prepared"] = () => _sequence.AppendPrepared("source", null!),
            ["prepared routed"] = () => _sequence.AppendPreparedThroughEventSource(typeof(object), "source", null!),
            ["routed"] = () => _sequence.AppendThroughEventSource(typeof(object), "source", new object()),
            ["routed batch"] = () => _sequence.AppendManyThroughEventSource(typeof(object), "source", Poison<object>()),
            ["batch"] = () => _sequence.AppendMany("source", Poison<object>()),
            ["named batch"] = () => _sequence.AppendManyWithNamedTags("source", Poison<object>(), Poison<NamedTag>()),
            ["labelled batch"] = () => _sequence.AppendMany(Poison<EventForEventSourceId>()),
            ["named labelled batch"] = () => _sequence.AppendManyWithNamedTags(Poison<EventForEventSourceId>(), Poison<NamedTag>())
        })
        {
            _errors.Add(name, await Catch.Exception(operation));
        }
    }
}
