// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Starts a host and makes sure a host that fails halfway through starting (a container up but the schema not) is
/// disposed instead of left running.
/// </summary>
public static class StartGuard
{
    /// <summary>Runs the start and disposes the host if it throws.</summary>
    /// <typeparam name="T">The host type.</typeparam>
    /// <param name="host">The host.</param>
    /// <param name="start">What starts the host.</param>
    /// <returns>The started host.</returns>
    public static async Task<T> StartOrDispose<T>(T host, Func<T, Task> start)
        where T : IAsyncDisposable
    {
        try
        {
            await start(host);
            return host;
        }
        catch (Exception)
        {
            await host.DisposeAsync();
            throw;
        }
    }
}
