// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision;

/// <summary>
/// Represents an <see cref="ISiloSupervisor"/> for a host that runs no supervisor, where nothing is ever run on a schedule.
/// </summary>
internal sealed class NoSiloSupervisor : ISiloSupervisor
{
    /// <summary>
    /// Gets the one instance.
    /// </summary>
    public static readonly NoSiloSupervisor Instance = new();

    /// <inheritdoc/>
    public void Register(string key, TimeSpan? interval, Func<Task<TimeSpan?>>? check, Func<Task>? afterDeparture)
    {
    }

    /// <inheritdoc/>
    public void Unregister(string key)
    {
    }
}
