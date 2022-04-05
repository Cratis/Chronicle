// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents a <see cref="IHostedService"/> for working with apapters.
/// </summary>
public class AdaptersService : IHostedService
{
    readonly IAdapters _adapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptersService"/> class.
    /// </summary>
    /// <param name="adapters"><see cref="IAdapters"/> to initialize.</param>
    public AdaptersService(IAdapters adapters)
    {
        _adapters = adapters;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _adapters.Initialize();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
