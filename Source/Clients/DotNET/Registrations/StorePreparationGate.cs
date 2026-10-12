// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Publishes preparation verdicts before local work and serializes registration with subsequent rediscovery.
/// </summary>
/// <param name="preparationFailed">Publishes the exact failure of the current local revision.</param>
internal sealed class StorePreparationGate(Action<Exception>? preparationFailed = null)
{
    readonly object _lock = new();
    readonly ArtifactPreparationLease _lease = new();
    readonly AsyncLocal<Ownership?> _ownership = new();
    Revision? _current;

    /// <summary>
    /// Gets the latest local preparation verdict; an untouched manual registry is valid.
    /// </summary>
    internal Task LatestVerdict
    {
        get
        {
            lock (_lock)
            {
                return _current?.Verdict ?? Task.CompletedTask;
            }
        }
    }

    /// <summary>
    /// Refuses callback reentry before callers can join a task that needs them to return.
    /// </summary>
    /// <exception cref="ArtifactPreparationReentered">The execution owns unfinished local work or the shared lease.</exception>
    internal void ThrowIfReentered()
    {
        if (_ownership.Value is { Active: true }) throw new ArtifactPreparationReentered();
    }

    /// <summary>
    /// Verifies that effects are running under the successful revision's registration lease.
    /// </summary>
    /// <exception cref="ArtifactPreparationReentered">Effects have no successful owning registration lease.</exception>
    internal void VerifyRegistrationLease()
    {
        if (_ownership.Value is not { Active: true, IsRegistration: true } owner ||
            !owner.Revision.Completion.Task.IsCompletedSuccessfully || owner.Revision.PayloadVerdict is not null)
        {
            throw new ArtifactPreparationReentered();
        }
    }

    /// <summary>
    /// Starts a new explicit revision after a completed revision, or joins one already pending.
    /// </summary>
    /// <param name="prepare">The local discovery operation.</param>
    /// <returns>The published preparation verdict.</returns>
    internal Task Discover(Func<Task> prepare)
    {
        ThrowIfReentered();
        var revision = GetOrStart(prepare, forceNew: true);
        return revision.Completion.Task;
    }

    /// <summary>
    /// Retains a deterministic payload failure on the successful revision that owns the registration lease.
    /// </summary>
    /// <param name="failure">The exact local payload failure.</param>
    /// <exception cref="ArtifactPreparationReentered">There is no active owning lease.</exception>
    internal void RecordPayloadFailure(Exception failure)
    {
        var owner = _ownership.Value;
        if (owner is not { Active: true }) throw new ArtifactPreparationReentered();
        lock (_lock)
        {
            if (owner.Revision.PayloadVerdict is not null) return;
            owner.Revision.PayloadVerdict = Task.FromException(failure);
            RecordFailure(owner.Revision, failure);
        }
    }

    /// <summary>
    /// Publishes an outcome atomically with local verdicts, so an older transport result cannot overwrite a newer local failure.
    /// </summary>
    /// <param name="publish">The outcome publication operation.</param>
    /// <param name="failure">The registration failure, or null for success.</param>
    internal void PublishRegistrationOutcome(Action<Exception?> publish, Exception? failure = null)
    {
        lock (_lock)
        {
            publish(_current?.Failure ?? failure);
        }
    }

    /// <summary>
    /// Awaits preparation before taking the registration lease, then rechecks superseding revisions before any callback or effect.
    /// </summary>
    /// <param name="prepare">Initial local preparation; the caller chooses automatic discovery or manual validation.</param>
    /// <param name="register">The complete payload-building and effect operation under a successful revision lease.</param>
    /// <returns>The registration operation.</returns>
    internal async Task Register(Func<Task> prepare, Func<Task> register)
    {
        ThrowIfReentered();
        while (true)
        {
            var revision = GetOrStart(prepare, forceNew: false);
            await revision.Verdict;
            await _lease.WaitAsync();
            var isCurrent = false;
            lock (_lock)
            {
                isCurrent = ReferenceEquals(_current, revision) && revision.PayloadVerdict is null;
            }
            if (!isCurrent)
            {
                _lease.Release();
                continue;
            }

            var previous = _ownership.Value;
            var owner = new Ownership(revision, isRegistration: true);
            _ownership.Value = owner;
            try
            {
                await register();
                return;
            }
            finally
            {
                owner.Active = false;
                _ownership.Value = previous;
                _lease.Release();
            }
        }
    }

    Revision GetOrStart(Func<Task> prepare, bool forceNew)
    {
        Revision revision;
        lock (_lock)
        {
            if (_current is not null && (!forceNew || !_current.Completion.Task.IsCompleted)) return _current;
            revision = new();
            _current = revision;
        }

        // The authoritative task is already visible; synchronous callbacks cannot expose a half-published revision.
        _ = Prepare(revision, prepare);
        return revision;
    }

    async Task Prepare(Revision revision, Func<Task> prepare)
    {
        await _lease.WaitAsync();
        var previous = _ownership.Value;
        var owner = new Ownership(revision, isRegistration: false);
        _ownership.Value = owner;
        try
        {
            await prepare();
            revision.Completion.TrySetResult();
        }
        catch (Exception exception)
        {
            lock (_lock)
            {
                RecordFailure(revision, exception);
                revision.Completion.TrySetException(exception);
            }
        }
        finally
        {
            owner.Active = false;
            _ownership.Value = previous;
            _lease.Release();
        }
    }

    void RecordFailure(Revision revision, Exception failure)
    {
        revision.Failure ??= failure;
        if (ReferenceEquals(_current, revision)) preparationFailed?.Invoke(revision.Failure);
    }

    sealed class Revision
    {
        internal volatile Task? PayloadVerdict;

        internal Exception? Failure { get; set; }

        internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Verdict => PayloadVerdict ?? Completion.Task;
    }

    sealed class Ownership(Revision revision, bool isRegistration)
    {
        internal volatile bool Active = true;

        internal Revision Revision { get; } = revision;

        internal bool IsRegistration { get; } = isRegistration;
    }
}
