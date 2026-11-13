// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Owns the reader host while bounding asynchronous disposal of synchronous services.
/// </summary>
/// <param name="connection">The kernel connection string.</param>
/// <param name="store">The isolated event store name.</param>
/// <param name="sideLedger">The independent reactor delivery ledger.</param>
internal sealed class BoundedHost(string connection, string store, SideLedger sideLedger) : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the owned reader host.
    /// </summary>
    internal IHost Host { get; } = WorkloadHost.Create("reader", connection, store, sideLedger);

    /// <inheritdoc/>
    public void Dispose() => Host.Dispose();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await Task.Run(Dispose).WaitAsync(TimeSpan.FromSeconds(10));
}
