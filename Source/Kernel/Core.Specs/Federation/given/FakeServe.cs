// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake acceptor: sends the scripted pull requests as frames, one at a time, and keeps the answers it gets.
/// </summary>
public class FakeServe : IFederationServe
{
    /// <summary>
    /// Gets the pull requests to send, in order. Every call replays the script.
    /// </summary>
    public List<ServePullRequest> Script { get; } = [];

    /// <summary>
    /// Gets the answers received, in order, across calls.
    /// </summary>
    public List<ServeBatch> Received { get; } = [];

    /// <summary>
    /// Gets or sets whether a call ends once the script is done. Otherwise it stays open until cancelled, like an acceptor with nothing to ask for.
    /// </summary>
    public bool EndAfterScript { get; set; }

    /// <summary>
    /// Gets how many calls were opened.
    /// </summary>
    public int Calls => _calls;

    readonly object _gate = new();
    readonly List<(Func<bool> Condition, TaskCompletionSource Source)> _waiters = [];
    int _calls;

    /// <summary>
    /// Waits until the given number of calls were opened.
    /// </summary>
    /// <param name="calls">The number of calls.</param>
    /// <returns>A task that completes when that many calls were opened.</returns>
    public Task WhenCalls(int calls) => When(() => _calls >= calls);

    /// <summary>
    /// Waits until the given number of answers were received.
    /// </summary>
    /// <param name="answers">The number of answers.</param>
    /// <returns>A task that completes when that many answers were received.</returns>
    public Task WhenReceived(int answers) => When(() =>
    {
        lock (Received)
        {
            return Received.Count >= answers;
        }
    });

    /// <inheritdoc/>
    public async IAsyncEnumerable<ServePullRequest> Serve(IAsyncEnumerable<ServeBatch> batches, CallContext callContext = default)
    {
        Interlocked.Increment(ref _calls);
        Notify();
        var cancellationToken = callContext.CancellationToken;
        await using var answers = batches.GetAsyncEnumerator(cancellationToken);
        foreach (var request in Script.ToArray())
        {
            yield return request;
            if (request.Status == FederatedEventBatchStatus.Denied)
            {
                yield break;
            }

            if (!await answers.MoveNextAsync())
            {
                yield break;
            }

            lock (Received)
            {
                Received.Add(answers.Current);
            }

            Notify();
        }

        if (!EndAfterScript)
        {
            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken));
            await cancelled.Task;
        }
    }

    Task When(Func<bool> condition)
    {
        lock (_gate)
        {
            if (condition())
            {
                return Task.CompletedTask;
            }

            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((condition, source));
            return source.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
    }

    void Notify()
    {
        lock (_gate)
        {
            foreach (var waiter in _waiters.Where(_ => _.Condition()).ToArray())
            {
                waiter.Source.TrySetResult();
                _waiters.Remove(waiter);
            }
        }
    }
}
