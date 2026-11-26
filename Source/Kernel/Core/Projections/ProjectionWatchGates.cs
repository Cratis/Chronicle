// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionWatchGates"/>.
/// </summary>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the gates are measured with; defaults to <see cref="TimeProvider.System"/>.</param>
[Singleton]
public class ProjectionWatchGates(TimeProvider? timeProvider = null) : IProjectionWatchGates
{
    /// <summary>
    /// How long a gate stays open unless it is renewed. It outlasts the watch refresh and the renewal throttle together.
    /// </summary>
    public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(90);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<ProjectionChangesetNotifierKey, ProjectionWatchGate> _gates = new();

    /// <inheritdoc/>
    public ProjectionWatchGate GateFor(ProjectionChangesetNotifierKey key) => _gates.GetOrAdd(key, static (_, provider) => new ProjectionWatchGate(provider), _timeProvider);

    /// <inheritdoc/>
    public void Open(ProjectionChangesetNotifierKey key) => GateFor(key).Open(TimeToLive);

    /// <inheritdoc/>
    public void Close(ProjectionChangesetNotifierKey key) => GateFor(key).Close();
}
