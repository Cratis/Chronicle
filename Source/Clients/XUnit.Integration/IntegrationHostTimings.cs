// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Measures access to lazily started integration hosts, separating the first start from reuse.
/// </summary>
internal static class IntegrationHostTimings
{
    static readonly ConditionalWeakTable<object, AccessState> _accesses = new();

    /// <summary>
    /// Measures an access to a host.
    /// </summary>
    /// <typeparam name="T">The type of the host.</typeparam>
    /// <param name="timings">The recorder.</param>
    /// <param name="factory">The factory owning the host.</param>
    /// <param name="perContext">Whether the host lives per context rather than shared.</param>
    /// <param name="access">The access to measure.</param>
    /// <returns>The accessed host.</returns>
    internal static T Measure<T>(IntegrationTimings timings, object factory, bool perContext, Func<T> access)
    {
        if (!timings.IsRecording) return access();
        var state = _accesses.GetValue(factory, static _ => new());
        var lifetime = perContext ? "per-context" : "shared";
        var phase = state.Started ? "reuse" : "start";
        var result = timings.Measure($"host-silo-{phase}-{lifetime}", access);
        state.MarkStarted();
        return result;
    }

    sealed class AccessState
    {
        int _started;

        internal bool Started => Volatile.Read(ref _started) != 0;

        internal void MarkStarted() => Volatile.Write(ref _started, 1);
    }
}
