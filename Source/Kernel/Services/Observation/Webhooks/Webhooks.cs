// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class Webhooks(IGrainFactory grainFactory, ILogger<Webhooks> logger) : IWebhooks
{
    /// <inheritdoc/>
    public async Task Register(RegisterWebhook request, CallContext context = default)
    {
        var definition = request.Webhook.ToChronicle();
        
    }
}
