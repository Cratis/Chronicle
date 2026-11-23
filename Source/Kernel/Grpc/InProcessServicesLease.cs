// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services;

/// <summary>
/// Releases the process-wide <see cref="SystemStoreGuard.InProcessServices"/> when the host that published it is disposed.
/// </summary>
/// <remarks>
/// Without this, the static keeps pointing at a disposed host and every in-process call that reads it fails with an
/// <see cref="ObjectDisposedException"/> until another host replaces it.
/// </remarks>
internal sealed class InProcessServicesLease : IDisposable
{
    IServiceProvider? _services;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_services is not null && ReferenceEquals(SystemStoreGuard.InProcessServices, _services))
        {
            SystemStoreGuard.InProcessServices = null;
        }

        _services = null;
    }

    /// <summary>
    /// Publishes the services as the in-process services and remembers them for release.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> of the host.</param>
    internal void Publish(IServiceProvider services)
    {
        _services = services;
        SystemStoreGuard.InProcessServices = services;
    }
}
