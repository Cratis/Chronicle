// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Reactive.Linq;

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.given;

public class a_live_query : Specification
{
    protected static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);
    protected readonly a_manual_time_provider _time = new();
    protected readonly a_recording_logger _logger = new();
    protected readonly Queue<Func<IEnumerable<int>>> _script = new();
    protected readonly List<int[]> _emissions = [];
    protected readonly TaskCompletionSource _firstEmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected IDisposable _subscription;
    protected Exception? _error;
    protected bool _completed;
    protected int _calls;
    protected int _failures;

    readonly MeterListener _listener = new();

    string QueryName => GetType().Name;

    protected void Subscribe() => _subscription = LiveQuery.Observe(Query, Interval, null, QueryName, _logger, _time).Subscribe(
        result =>
        {
            _emissions.Add(result.ToArray());
            _firstEmission.TrySetResult();
        },
        error =>
        {
            _error = error;
            _firstEmission.TrySetException(error);
        },
        () => _completed = true);

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "chronicle-sql-live-query-poll-failures")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((_, value, tags, _) =>
        {
            if (tags.ToArray().Any(tag => tag.Key == "query" && Equals(tag.Value, QueryName)))
            {
                Interlocked.Add(ref _failures, value);
            }
        });
        _listener.Start();
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _listener.Dispose();
    }

    Task<IEnumerable<int>> Query()
    {
        Interlocked.Increment(ref _calls);
        var next = _script.Count > 1 ? _script.Dequeue() : _script.Peek();
        return Task.FromResult(next());
    }
}
