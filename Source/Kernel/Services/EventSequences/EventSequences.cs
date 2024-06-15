// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Identities;

namespace Cratis.Chronicle.Services.EventSequences;

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
        var eventSequence = GetEventSequence(request.EventStoreName, request.Namespace, request.EventSequenceId);
        await eventSequence.Append(
            request.EventSourceId,
            request.EventType.ToKernel(),
            JsonSerializer.Deserialize<JsonNode>(request.Content, jsonSerializerOptions)!.AsObject(),
            request.Causation.ToKernel(),
            request.Identity.ToKernel(),
            request.ValidFrom);

        return new AppendResponse();
    }

    Grains.EventSequences.IEventSequence GetEventSequence(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        grainFactory.GetGrain<Grains.EventSequences.IEventSequence>(eventSequenceId, keyExtension: new EventStoreAndNamespace(eventStore, @namespace));
}
