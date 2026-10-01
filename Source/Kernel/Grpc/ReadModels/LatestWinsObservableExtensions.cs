// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Extension methods for observables that process their emissions one at a time while only ever keeping the latest one waiting.
/// </summary>
internal static class LatestWinsObservableExtensions
{
    /// <summary>
    /// Project each emission through an asynchronous selector, one emission at a time and in order, keeping at most one
    /// emission waiting while another is being processed.
    /// </summary>
    /// <typeparam name="TSource">The type of the source emissions.</typeparam>
    /// <typeparam name="TResult">The type of the results.</typeparam>
    /// <param name="source">The source <see cref="IObservable{T}"/>.</param>
    /// <param name="selector">The asynchronous selector to project an emission through.</param>
    /// <param name="logger">The <see cref="ILogger"/> for reporting failures that cannot be passed on to the observer.</param>
    /// <returns>An <see cref="IObservable{T}"/> of the projected results.</returns>
    /// <remarks>
    /// An emission that arrives while an earlier one is being processed replaces any emission still waiting, so the
    /// waiting one is never processed. Results therefore stay in the order of their emissions, the work and memory
    /// stay bounded however fast the source emits, and the last emission is always processed. A completion or an error of the
    /// source is passed on after the emission being processed has been delivered, and the one waiting is dropped on an error.
    /// A failure of the selector is passed on as an error. A failure of the observer itself is never passed back to it; it
    /// is logged and the subscription is ended.
    /// </remarks>
    internal static IObservable<TResult> SelectLatestSequentially<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, Task<TResult>> selector,
        ILogger logger) =>
        Observable.Create<TResult>(observer =>
        {
            var gate = new object();
            var cancellation = new CancellationDisposable();
            var isProcessing = false;
            var hasWaiting = false;
            TSource? waiting = default;
            var isCompleted = false;
            Exception? error = null;
            var subscription = new SingleAssignmentDisposable();
            var subscriptions = new CompositeDisposable(subscription, cancellation);

            // Process runs unobserved, so nothing may escape it: a failure of the observer is logged and ends the subscription.
            async Task Process(TSource first)
            {
                try
                {
                    await ProcessEmissions(first).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.FailedToDeliverToObserver(ex);
                    subscriptions.Dispose();
                }
            }

            async Task ProcessEmissions(TSource first)
            {
                var current = first;
                while (true)
                {
                    TResult result;
                    try
                    {
                        result = await selector(current).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!cancellation.IsDisposed)
                        {
                            observer.OnError(ex);
                        }

                        return;
                    }

                    if (cancellation.IsDisposed)
                    {
                        return;
                    }

                    observer.OnNext(result);

                    Exception? pendingError;
                    bool pendingCompletion;
                    lock (gate)
                    {
                        if (hasWaiting && error is null)
                        {
                            current = waiting!;
                            waiting = default;
                            hasWaiting = false;
                            continue;
                        }

                        isProcessing = false;
                        pendingError = error;
                        pendingCompletion = isCompleted;
                    }

                    if (cancellation.IsDisposed)
                    {
                        return;
                    }

                    if (pendingError is not null)
                    {
                        observer.OnError(pendingError);
                    }
                    else if (pendingCompletion)
                    {
                        observer.OnCompleted();
                    }

                    return;
                }
            }

            subscription.Disposable = source.Subscribe(
                item =>
                {
                    lock (gate)
                    {
                        if (isProcessing)
                        {
                            waiting = item;
                            hasWaiting = true;
                            return;
                        }

                        isProcessing = true;
                    }

                    _ = Process(item);
                },
                ex =>
                {
                    lock (gate)
                    {
                        if (isProcessing)
                        {
                            error = ex;
                            hasWaiting = false;
                            waiting = default;
                            return;
                        }
                    }

                    observer.OnError(ex);
                },
                () =>
                {
                    lock (gate)
                    {
                        if (isProcessing)
                        {
                            isCompleted = true;
                            return;
                        }
                    }

                    observer.OnCompleted();
                });

            return subscriptions;
        });
}
