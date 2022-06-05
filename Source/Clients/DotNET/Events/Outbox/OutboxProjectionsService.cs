// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Represents a <see cref="IHostedService"/> for working with outbox projections.
/// </summary>
public class OutboxProjectionsService : IHostedService
{
    readonly IOutboxProjectionsRegistrar _outboxProjectionsRegistrar;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionsService"/> class.
    /// </summary>
    /// <param name="outboxProjectionsRegistrar">The <see cref="IOutboxProjectionsRegistrar"/> system.</param>
    public OutboxProjectionsService(IOutboxProjectionsRegistrar outboxProjectionsRegistrar)
    {
        _outboxProjectionsRegistrar = outboxProjectionsRegistrar;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _outboxProjectionsRegistrar.DiscoverAndRegisterAll();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
