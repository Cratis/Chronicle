// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Owns a backing container independently of kernel container rebuilds.
/// </summary>
/// <param name="build">Builds the backing container.</param>
/// <param name="start">Starts the backing container.</param>
/// <param name="built">Observes each built backing container.</param>
public sealed class ReusableBackingContainer(Func<IContainer> build, Func<IContainer, Task> start, Action<IContainer> built) : IAsyncDisposable
{
    IContainer? _container;

    /// <summary>
    /// Starts the backing container once and reuses it for later kernel attempts.
    /// </summary>
    /// <returns>The started backing container.</returns>
    public async Task<IContainer> GetOrStart()
    {
        if (_container is not null) return _container;
        var container = build();
        try
        {
            built(container);
            await start(container);
            _container = container;
            return container;
        }
        catch (Exception failure)
        {
            try
            {
                await container.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                XUnit.Integration.KernelFixtureCertificate.ReportCleanupFailure(failure, cleanup);
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not { } container) return;
        await container.DisposeAsync();
        _container = null;
    }
}
