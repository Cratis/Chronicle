// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Keeps invocation enforcement alive until its first caller disposes the owning frame.
/// Subsequent callers receive leases that cannot end enforcement.
/// </summary>
public sealed class ReadProtectionFrame : IDisposable
{
    static readonly AsyncLocal<ReadProtectionFrame?> _current = new();
    static readonly AsyncLocal<CompletionPermission?> _completion = new();
    readonly ReadProtectionFrame _root;
    readonly Func<IEventSequence> _sequence;
    readonly Func<ReadProtectionTarget> _target;
    readonly bool _ownsLifetime;
    bool _active = true;
    bool _commitSucceeded;

    /// <summary>
    /// Initializes the owning invocation frame.
    /// </summary>
    /// <param name="unit">The invocation's unit.</param>
    /// <param name="sequence">The unit's bound event sequence.</param>
    /// <param name="target">The unit's complete target identity.</param>
    internal ReadProtectionFrame(IUnitOfWork unit, Func<IEventSequence> sequence, Func<ReadProtectionTarget> target)
    {
        UnitOfWork = unit;
        _root = this;
        _sequence = sequence;
        _target = target;
        _ownsLifetime = true;
        _current.Value = this;
    }

    ReadProtectionFrame(ReadProtectionFrame owner)
    {
        _root = owner._root;
        UnitOfWork = _root.UnitOfWork;
        _sequence = _root._sequence;
        _target = _root._target;
    }

    /// <summary>
    /// Gets the invocation's unit, including after ambient completion clears it.
    /// </summary>
    public IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets whether the invocation's unit completed.
    /// </summary>
    public bool UnitCompleted => UnitOfWork.IsCompleted;

    /// <summary>
    /// Gets the active frame in this asynchronous flow.
    /// </summary>
    internal static ReadProtectionFrame? Current => _current.Value is { } frame && Volatile.Read(ref frame._root._active) ? frame._root : null;

    /// <summary>
    /// Runs framework stream completion after a successful owner commit.
    /// No read or other immediate append is permitted by this capability.
    /// </summary>
    /// <param name="completion">The stream completion on the unit's target.</param>
    /// <returns>The completion task.</returns>
    /// <exception cref="ImmediateAppendWhileReadsAreProtected">The invocation did not successfully commit or no longer owns the active frame.</exception>
    public async Task RunAfterCommit(Func<IEventSequence, Task> completion)
    {
        if (!ReferenceEquals(Current, _root) || !UnitCompleted || !_root._commitSucceeded)
        {
            throw new ImmediateAppendWhileReadsAreProtected(nameof(RunAfterCommit), "stream completion before a successful owner commit");
        }
        var previous = _completion.Value;
        var permission = new CompletionPermission(_root);
        _completion.Value = permission;
        try
        {
            await completion(_sequence());
        }
        finally
        {
            permission.Active = false;
            if (ReferenceEquals(_completion.Value, permission)) _completion.Value = previous;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_ownsLifetime) return;
        Volatile.Write(ref _active, false);
        if (ReferenceEquals(_current.Value, this)) _current.Value = null;
    }

    /// <summary>
    /// Checks the narrow completion permission against the actual target.
    /// </summary>
    /// <param name="target">The sequence receiving CompleteStream.</param>
    /// <returns>Whether this flow may complete that target.</returns>
    internal static bool CanComplete(ReadProtectionTarget target) =>
        _completion.Value is { Active: true } permission && ReferenceEquals(Current, permission.Frame) && permission.Frame._target() == target;

    /// <summary>
    /// Creates a lease that cannot end the invocation's enforcement.
    /// </summary>
    /// <returns>The borrowed frame.</returns>
    internal ReadProtectionFrame Borrow() => new(_root);

    /// <summary>
    /// Records successful owner completion, independently of rollback and disposal.
    /// </summary>
    internal void CommittedSuccessfully() => _root._commitSucceeded = true;

    sealed class CompletionPermission(ReadProtectionFrame frame)
    {
        public ReadProtectionFrame Frame { get; } = frame;
        public bool Active { get; set; } = true;
    }
}
