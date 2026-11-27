// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>Shares one bounded attempt budget across protected writes and receipt-collision recovery.</summary>
/// <param name="clock">The clock used by deadline cancellation and deterministic specifications.</param>
/// <param name="delay">The cancellable backoff, never called before the first attempt.</param>
/// <param name="jitterMilliseconds">The jitter source, clamped to the documented 25–100 millisecond range.</param>
public sealed class NamespaceCommitRetries(TimeProvider clock, Func<TimeSpan, CancellationToken, Task> delay, Func<int> jitterMilliseconds)
{
    /// <summary>The maximum attempts, including the initial transaction and collision recovery.</summary>
    public const int MaximumAttempts = 3;

    int _attempts;

    /// <summary>Initializes production backoff using a bounded, thread-safe jitter source.</summary>
    public NamespaceCommitRetries() : this(TimeProvider.System, Task.Delay, () => Random.Shared.Next(25, 101))
    {
    }

    /// <summary>Gets whether another transaction may begin.</summary>
    public bool HasRemainingAttempts => _attempts < MaximumAttempts;

    /// <summary>Links the original caller deadline without issuing or refreshing a namespace identity.</summary>
    /// <param name="context">The original context, also used by collision recovery.</param>
    /// <returns>A disposable cancellation scope for the invocation.</returns>
    public NamespaceCommitCancellation Bind(NamespaceCommitContext context) => new(context, clock);

    /// <summary>Consumes an attempt only after cancellable backoff and a fresh deadline check.</summary>
    /// <param name="lifetime">The unchanged caller's cancellation scope.</param>
    /// <returns>The completed admission to begin one transaction.</returns>
    /// <exception cref="InvalidOperationException">All three attempts were already consumed.</exception>
    public async Task BeginAttempt(NamespaceCommitCancellation lifetime)
    {
        lifetime.ThrowIfCancellationRequested();
        if (!HasRemainingAttempts) throw new InvalidOperationException("The namespace transaction attempt budget is exhausted.");
        if (_attempts > 0) await delay(TimeSpan.FromMilliseconds(Math.Clamp(jitterMilliseconds(), 25, 100)), lifetime.Token);
        lifetime.ThrowIfCancellationRequested();
        _attempts++;
    }
}
