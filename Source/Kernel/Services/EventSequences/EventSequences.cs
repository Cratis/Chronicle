// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Auditing;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Identities;
using Cratis.Kernel.Contracts.EventSequences;

namespace Cratis.Kernel.Services.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequences"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
public class EventSequences(
    IGrainFactory grainFactory,
    JsonSerializerOptions jsonSerializerOptions) : IEventSequences
{
    /// <inheritdoc/>
    public async Task<AppendResponse> Append(AppendRequest request)
    {
        var eventSequence = GetEventSequence(request.EventStoreName, request.EventSequenceId, request.Namespace);
        await eventSequence.Append(
            request.EventSourceId,
            request.EventType.ToKernel(),
            JsonSerializer.Deserialize<JsonNode>(request.Content, jsonSerializerOptions)!.AsObject(),
            request.Causation.ToKernel(),
            request.Identity.ToKernel(),
            request.ValidFrom);

        return new AppendResponse();
    }

    Grains.EventSequences.IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        grainFactory.GetGrain<Grains.EventSequences.IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
}
