// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Refuses immediate appends in this asynchronous flow until the first caller disposes its frame.
/// That frame owns completion only when the unit has no existing owner; subsequent acquisitions borrow its lifetime.
/// </summary>
public sealed class ImmediateAppendRefusal : IDisposable
{
    static readonly AsyncLocal<ImmediateAppendRefusal?> _current = new();
    readonly ImmediateAppendRefusal _root;
    readonly ImmediateAppendRefusal? _previous;
    readonly Func<ImmediateAppendRefusal, Task> _commit;
    readonly Func<ImmediateAppendRefusal, Task> _rollback;
    readonly Action<ImmediateAppendRefusal> _dispose;
    readonly bool _ownsLifetime;
    readonly bool _ownsCompletion;
    bool _active = true;

    /// <summary>
    /// Opens the refusal lifetime synchronously in the caller's flow.
    /// </summary>
    /// <param name="unitOfWork">The unit whose appends are refused.</param>
    /// <param name="ownsCompletion">Whether this frame, rather than an existing owner, completes the unit.</param>
    /// <param name="commit">The owner's trusted commit.</param>
    /// <param name="rollback">The owner's rollback.</param>
    /// <param name="dispose">Cleanup for the refusal lifetime.</param>
    internal ImmediateAppendRefusal(
        IUnitOfWork unitOfWork,
        bool ownsCompletion,
        Func<ImmediateAppendRefusal, Task> commit,
        Func<ImmediateAppendRefusal, Task> rollback,
        Action<ImmediateAppendRefusal> dispose)
    {
        UnitOfWork = unitOfWork;
        _root = this;
        _previous = _current.Value;
        _commit = commit;
        _rollback = rollback;
        _dispose = dispose;
        _ownsLifetime = true;
        _ownsCompletion = ownsCompletion;
        _current.Value = this;
    }

    ImmediateAppendRefusal(ImmediateAppendRefusal owner)
    {
        _root = owner._root;
        UnitOfWork = _root.UnitOfWork;
        _commit = _root._commit;
        _rollback = _root._rollback;
        _dispose = _root._dispose;
        _current.Value = _root;
    }

    /// <summary>
    /// Gets the unit that owns this frame, including after ambient completion clears the unit.
    /// </summary>
    public IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets the active refusal independently of the ambient unit and read protection.
    /// </summary>
    internal static ImmediateAppendRefusal? Current =>
        _current.Value is { } frame && Volatile.Read(ref frame._root._active) ? frame._root : null;

    /// <summary>
    /// Commits the owning unit without admitting immediate appends from callbacks or serializers.
    /// </summary>
    /// <returns>The commit task. Inspect the unit's result after awaiting it.</returns>
    /// <exception cref="ImmediateAppendRefusalRequiresOwner">This frame is refusal-only, borrowed, disposed, or not active in this flow.</exception>
    public Task Commit()
    {
        RequireCompletionOwner();
        return _commit(this);
    }

    /// <summary>
    /// Rolls back the owning unit without ending append refusal.
    /// </summary>
    /// <returns>The rollback task.</returns>
    /// <exception cref="ImmediateAppendRefusalRequiresOwner">This frame is refusal-only, borrowed, disposed, or not active in this flow.</exception>
    public Task Rollback()
    {
        RequireCompletionOwner();
        return _rollback(this);
    }

    /// <summary>
    /// Rolls back an unfinished unit only if this frame owns completion, then restores the previous flow state.
    /// Refusal-only frames leave completion to the existing owner. Borrowed frames do nothing.
    /// </summary>
    /// <exception cref="ImmediateAppendRefusalRequiresOwner">The frame does not own the active lifetime in this flow.</exception>
    /// <exception cref="UnitOfWorkIsCompleting">The unit still has a commit in flight; refusal remains active.</exception>
    public void Dispose()
    {
        if (!_ownsLifetime || !Volatile.Read(ref _active)) return;
        RequireLifetimeOwner();
        try
        {
            _dispose(this);
        }
        finally
        {
            // A rejected in-flight disposal must keep refusal; a failing completion callback must not.
            if (!_ownsCompletion || UnitOfWork.IsCompleted)
            {
                Volatile.Write(ref _active, false);
                if (ReferenceEquals(_current.Value, this)) _current.Value = _previous;
            }
        }
    }

    /// <summary>
    /// Acquires a lease that cannot complete the unit or end refusal.
    /// </summary>
    /// <returns>The borrowed lifetime.</returns>
    internal ImmediateAppendRefusal Borrow() => new(_root);

    void RequireCompletionOwner()
    {
        RequireLifetimeOwner();
        if (!_ownsCompletion) throw new ImmediateAppendRefusalRequiresOwner();
    }

    void RequireLifetimeOwner()
    {
        if (!_ownsLifetime || !ReferenceEquals(Current, this)) throw new ImmediateAppendRefusalRequiresOwner();
    }
}
