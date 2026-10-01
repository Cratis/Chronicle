// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Marks the asynchronous flow in which the kernel executes one of its own commands through the Arc command pipeline.
/// </summary>
/// <remarks>
/// A kernel hosted in-process shares its host - and that host's Arc type discovery - with an application. The marker
/// lets <see cref="KernelSideInstancesOf{T}"/> tell the kernel's own command executions apart from the application's,
/// so the kernel's commands see only the extension points a stand-alone kernel host would discover, while the
/// application's commands keep seeing everything.
/// </remarks>
internal static class KernelCommandExecution
{
    static readonly AsyncLocal<bool> _active = new();

    /// <summary>
    /// Gets a value indicating whether the current asynchronous flow is executing a kernel command.
    /// </summary>
    internal static bool IsActive => _active.Value;

    /// <summary>
    /// Marks the current asynchronous flow as executing a kernel command until the returned scope is disposed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that restores the previous state when disposed.</returns>
    internal static IDisposable Begin()
    {
        var previous = _active.Value;
        _active.Value = true;
        return new Scope(previous);
    }

    sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => _active.Value = previous;
    }
}
