// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Chronicle.Benchmarks.ColdStart")]

namespace Cratis.Chronicle;

/// <summary>
/// Local diagnostic only. A null <see cref="Recorder"/> is the disabled control: no recorder calls are made.
/// </summary>
internal static class StartupDiagnostics
{
    /// <summary>
    /// Gets or sets the active recorder, or null when disabled.
    /// </summary>
    internal static volatile StartupDiagnosticRecorder? Recorder;

    /// <summary>
    /// Runs an operation that returns its original task, observing that task's completion when enabled.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="operation">The operation to attribute.</param>
    /// <param name="state">The state passed to <paramref name="begin"/>.</param>
    /// <param name="begin">The operation start; its synchronous exceptions and task are returned unchanged.</param>
    /// <returns>The original task.</returns>
    internal static Task Trace<TState>(StartupOperation operation, TState state, Func<TState, Task> begin)
    {
        // The scope is owned by the task continuation, which publishes and releases it on completion.
#pragma warning disable CA2000
        var scope = Recorder?.Start(operation);
#pragma warning restore CA2000
        return scope is null ? begin(state) : scope.Run(() => begin(state));
    }
}
