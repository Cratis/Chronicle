// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Coordinates discovered global seeds across a client's namespace stores for one connection lifetime.
/// </summary>
internal sealed class GlobalSeedRegistration
{
    ConcurrentDictionary<EventStoreName, Registration> _stores = new();

    /// <summary>
    /// Starts a fresh connection lifetime without allowing an old in-flight send to mark it registered.
    /// </summary>
    /// <returns>The completed reset task.</returns>
    internal Task Reset()
    {
        Interlocked.Exchange(ref _stores, new());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Serializes namespace stores and records global registration only after a successful send.
    /// </summary>
    /// <param name="name">The event store name.</param>
    /// <param name="register">Sends entries, including globals if requested, and reports whether globals were sent.</param>
    /// <returns>The registration task.</returns>
    internal async Task Run(EventStoreName name, Func<bool, Task<bool>> register)
    {
        var registration = _stores.GetOrAdd(name, _ => new());
        await registration.Gate.WaitAsync();
        try
        {
            registration.Sent |= await register(!registration.Sent);
        }
        finally
        {
            registration.Gate.Release();
        }
    }

    sealed class Registration
    {
        internal SemaphoreSlim Gate { get; } = new(1, 1);
        internal bool Sent { get; set; }
    }
}
