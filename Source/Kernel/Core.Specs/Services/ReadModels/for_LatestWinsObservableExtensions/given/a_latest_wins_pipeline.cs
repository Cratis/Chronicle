// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.ReadModels.for_LatestWinsObservableExtensions.given;

/// <summary>
/// Base context with a source that is projected through <see cref="LatestWinsObservableExtensions.SelectLatestSequentially{TSource, TResult}"/>
/// and an observer that records everything it is told.
/// </summary>
public class a_latest_wins_pipeline : Specification
{
    protected static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Holds the first item until completed. Deliberately not RunContinuationsAsynchronously: completing the gate then runs the
    /// rest of the item's processing inline, so once SetResult has returned the specs can assert without waiting on the clock.
    /// </summary>
    protected readonly TaskCompletionSource<int> _gate = new();
    protected readonly TaskCompletionSource _terminated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly List<int> _processed = [];
    protected readonly List<string> _events = [];
    readonly object _sync = new();
    protected Subject<int> _source;
    protected CapturingLogger _logger;

    void Establish()
    {
        _source = new();
        _logger = new();
    }

    /// <summary>
    /// Subscribe to the source projected through a selector that multiplies by ten, with the first item held until the gate is completed.
    /// </summary>
    /// <param name="onNext">Optional action to run when a result is delivered.</param>
    /// <returns>The subscription.</returns>
    protected IDisposable SubscribeWithGatedFirstItem(Action<int>? onNext = null) => Subscribe(
        item => item == 1 ? _gate.Task : Task.FromResult(item * 10),
        onNext);

    /// <summary>
    /// Subscribe to the source projected through the given selector.
    /// </summary>
    /// <param name="selector">The selector to project each item through.</param>
    /// <param name="onNext">Optional action to run when a result is delivered.</param>
    /// <returns>The subscription.</returns>
    protected IDisposable Subscribe(Func<int, Task<int>> selector, Action<int>? onNext = null) =>
        _source.SelectLatestSequentially(
            item =>
            {
                lock (_sync)
                {
                    _processed.Add(item);
                }

                return selector(item);
            },
            _logger).Subscribe(
            result =>
            {
                Record($"next:{result}");
                onNext?.Invoke(result);
            },
            error =>
            {
                Record($"error:{error.Message}");
                _terminated.TrySetResult();
            },
            () =>
            {
                Record("completed");
                _terminated.TrySetResult();
            });

    void Record(string @event)
    {
        lock (_sync)
        {
            _events.Add(@event);
        }
    }

    /// <summary>
    /// An <see cref="ILogger"/> that remembers the exceptions it was asked to log.
    /// </summary>
    protected sealed class CapturingLogger : ILogger
    {
        readonly object _sync = new();

        public List<Exception> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null)
            {
                lock (_sync)
                {
                    Exceptions.Add(exception);
                }
            }
        }
    }
}
