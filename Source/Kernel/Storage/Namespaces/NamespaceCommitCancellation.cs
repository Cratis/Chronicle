// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>Links one immutable caller deadline to the activation lifetime without changing the bound identity.</summary>
public sealed class NamespaceCommitCancellation : IDisposable
{
    readonly TimeProvider _clock;
    readonly CancellationTokenSource? _deadline;
    readonly CancellationTokenSource? _linked;

    /// <summary>Initializes a cancellation scope for one transaction invocation.</summary>
    /// <param name="context">The unchanged caller lifetime and optional absolute deadline.</param>
    /// <param name="clock">The clock observing that deadline.</param>
    public NamespaceCommitCancellation(NamespaceCommitContext context, TimeProvider clock)
    {
        Context = context;
        _clock = clock;
        if (context.Deadline is not { } deadline)
        {
            Token = context.ActivationCancellation;
            return;
        }
        var remaining = deadline - clock.GetUtcNow();
        _deadline = new(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, clock);
        _linked = CancellationTokenSource.CreateLinkedTokenSource(context.ActivationCancellation, _deadline.Token);
        Token = _linked.Token;
        if (remaining <= TimeSpan.Zero) _deadline.Cancel();
    }

    /// <summary>Gets the original immutable context, including its unrefreshed incarnation and deadline.</summary>
    public NamespaceCommitContext Context { get; }

    /// <summary>Gets the token passed to marker, event, receipt and commit operations.</summary>
    public CancellationToken Token { get; }

    /// <summary>Checks cancellation and the absolute deadline at an attempt or commit boundary.</summary>
    /// <exception cref="OperationCanceledException">The activation or caller deadline no longer admits work.</exception>
    public void ThrowIfCancellationRequested()
    {
        if (Context.Deadline <= _clock.GetUtcNow()) _deadline?.Cancel();
        Token.ThrowIfCancellationRequested();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _linked?.Dispose();
        _deadline?.Dispose();
    }
}
