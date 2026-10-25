// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Diagnostics;

/// <summary>
/// Aggregates EF Core command timings. IDs and numeric totals only: never retain a context, connection, command, SQL text or parameters.
/// </summary>
/// <param name="timings">The timings receiving the aggregate.</param>
/// <param name="enabled">Overrides whether timings are enabled; defaults to the environment switch.</param>
internal sealed class SqlCommandTimings(IntegrationTimings timings, bool? enabled = null) : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    const int MaximumPending = 4096;
    readonly bool _enabled = enabled ?? IntegrationTimings.Enabled;
    readonly HashSet<Guid> _contexts = [];
    readonly HashSet<Guid> _pending = [];
    readonly List<IDisposable> _listeners = [];
    IDisposable? _subscription;
    bool _incomplete;
    bool _disposed;
    int _count;
    int _failures;
    double _duration;

    internal void Start()
    {
        if (_enabled && _subscription is null) _subscription = DiagnosticListener.AllListeners.Subscribe(this);
    }

    internal T Track<T>(T context)
        where T : DbContext
    {
        if (!_enabled) return context;
        lock (_pending)
        {
            if (_contexts.Count < MaximumPending) _contexts.Add(context.ContextId.InstanceId);
            else _incomplete = true;
        }
        return context;
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name != "Microsoft.EntityFrameworkCore") return;
        lock (_pending)
        {
            if (_disposed) return;
            if (_listeners.Count < 8) _listeners.Add(value.Subscribe(this, IsCommandEvent));
            else _incomplete = true;
        }
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        try
        {
            if (!IsCommandEvent(value.Key)) return;
            if (value.Value is not CommandEventData data || data.Context is null) return;
            lock (_pending)
            {
                if (_disposed || !_contexts.Contains(data.Context.ContextId.InstanceId)) return;
                RecordCommand(value.Key, data);
            }
        }
        catch
        {
            // A diagnostics callback is never allowed to change a database operation's outcome.
            lock (_pending) _incomplete = true;
        }
    }

    void RecordCommand(string name, CommandEventData data)
    {
        if (name == RelationalEventId.CommandExecuting.Name)
        {
            if (_pending.Count >= MaximumPending || !_pending.Add(data.CommandId)) _incomplete = true;
            return;
        }
        if (data is CommandEndEventData end) RecordEnd(name, end);
    }

    void RecordEnd(string name, CommandEndEventData data)
    {
        if (!_pending.Remove(data.CommandId))
        {
            _incomplete = true;
            return;
        }
        _count++;
        _duration += data.Duration.TotalMilliseconds;
        if (name != RelationalEventId.CommandExecuted.Name) _failures++;
    }

    static bool IsCommandEvent(string name) =>
        name == RelationalEventId.CommandExecuting.Name || name == RelationalEventId.CommandExecuted.Name ||
        name == RelationalEventId.CommandError.Name || name == RelationalEventId.CommandCanceled.Name;

    public void OnError(Exception error)
    {
        lock (_pending) _incomplete = true;
    }

    public void OnCompleted() { }

    public void Dispose()
    {
        lock (_pending)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _subscription?.Dispose();
        foreach (var listener in _listeners) listener.Dispose();
        lock (_pending)
        {
            timings.Aggregate("ef-command-duration", new(_duration, _count, _failures, !_incomplete && _count > 0 && _pending.Count == 0));
            _pending.Clear();
            _contexts.Clear();
            _listeners.Clear();
        }
    }
}
