// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle;

/// <summary>
/// Local diagnostic only. A bounded, numeric span recorder. Reservation, publication and capture all use one gate,
/// so a capture is a single consistent view: records, drop count and active count are never observed torn.
/// </summary>
/// <param name="capacity">The maximum number of spans retained.</param>
internal sealed class StartupDiagnosticRecorder(int capacity)
{
    readonly object _gate = new();
    readonly StartupDiagnosticRecord[] _records = new StartupDiagnosticRecord[capacity];
    readonly AsyncLocal<long> _parent = new();
    long _attempts;
    int _active;

    /// <summary>
    /// Gets the maximum number of spans retained.
    /// </summary>
    internal int Capacity => _records.Length;

    /// <summary>
    /// Gets the number of spans lost to overflow.
    /// </summary>
    internal long Dropped => Capture().Dropped;

    /// <summary>
    /// Gets the number of reserved spans not yet published.
    /// </summary>
    internal int Active => Capture().Active;

    /// <summary>
    /// Starts a span.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns>The scope, or null when the buffer is full (overflow invalidates attribution but never throws).</returns>
    internal Scope? Start(StartupOperation operation)
    {
        long id;
        lock (_gate)
        {
            id = ++_attempts;
            if (id > Capacity)
            {
                return null;
            }
            _active++;
        }
        var parent = _parent.Value;
        var scope = new Scope(this, id, parent, operation, Stopwatch.GetTimestamp());
        _parent.Value = id;
        return scope;
    }

    /// <summary>
    /// Captures the recorder deterministically. Scopes still active are reported as active (unpublished slots have
    /// outcome zero) and invalidate the capture; they are never silently omitted or copied while being written.
    /// </summary>
    /// <returns>The consistent capture.</returns>
    internal StartupDiagnosticSnapshot Capture()
    {
        lock (_gate)
        {
            var reserved = (int)Math.Min(_attempts, Capacity);
            return new(_records.AsSpan(0, reserved).ToArray(), Math.Max(0, _attempts - Capacity), _active, Capacity);
        }
    }

    /// <summary>
    /// Gets the reserved records of a consistent capture.
    /// </summary>
    /// <returns>The records.</returns>
    internal StartupDiagnosticRecord[] Snapshot() => Capture().Records;

    /// <summary>
    /// Local diagnostic only. One in-flight span.
    /// </summary>
    /// <param name="recorder">The owning recorder.</param>
    /// <param name="id">The reservation identifier.</param>
    /// <param name="parent">The enclosing span identifier.</param>
    /// <param name="operation">The operation.</param>
    /// <param name="start">The monotonic start tick.</param>
    internal sealed class Scope(StartupDiagnosticRecorder recorder, long id, long parent, StartupOperation operation, long start) : IDisposable
    {
        int _outcome = 2;
        int _ended;

        /// <inheritdoc/>
        public void Dispose()
        {
            recorder._parent.Value = parent;
            End();
        }

        /// <summary>
        /// Marks the original operation as successfully finished.
        /// </summary>
        internal void Complete() => _outcome = 1;

        /// <summary>
        /// Starts an operation and observes the exact task it returns, never awaiting or replacing it.
        /// A synchronous exception ends the span as failed and propagates unchanged.
        /// </summary>
        /// <param name="begin">The operation start.</param>
        /// <returns>The original task.</returns>
        internal Task Run(Func<Task> begin)
        {
            Task task;
            try
            {
                task = begin();
            }
            catch
            {
                Dispose();
                throw;
            }
            Observe(task);
            return task;
        }

        /// <summary>
        /// Observes a task's completion without awaiting or replacing it.
        /// </summary>
        /// <param name="task">The task to observe.</param>
        internal void Observe(Task task)
        {
            recorder._parent.Value = parent;
            _ = task.ContinueWith(Finish, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        static int OutcomeOf(Task completed)
        {
            if (completed.IsCompletedSuccessfully)
            {
                return 1;
            }
            return completed.IsCanceled ? 3 : 2;
        }

        void Finish(Task completed)
        {
            _outcome = OutcomeOf(completed);
            End();
        }

        void End()
        {
            if (Interlocked.Exchange(ref _ended, 1) != 0)
            {
                return;
            }
            var end = Stopwatch.GetTimestamp();
            lock (recorder._gate)
            {
                recorder._records[(int)id - 1] = new(id, parent, operation, start, end, _outcome);
                recorder._active--;
            }
        }
    }
}
