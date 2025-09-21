// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhooks"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
internal sealed class Webhooks(IGrainFactory grainFactory) : IWebhooks
{
    /// <inheritdoc/>
    public Task Register(RegisterWebhook request, CallContext context = default)
    {
        var webhooksManager = grainFactory.GetGrain<Grains.Observation.Webhooks.IWebhooksManager>(request.EventStore);
        var webhooks = request.Webhooks.Select(w => w.ToChronicle()).ToArray();

        _ = Task.Run(() => webhooksManager.Register(webhooks));
        return Task.CompletedTask;
    }
}
