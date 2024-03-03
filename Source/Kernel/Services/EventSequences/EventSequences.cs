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
public class EventSequences : IEventSequences
{
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequences"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    public EventSequences(
        IGrainFactory grainFactory,
        IExecutionContextManager executionContextManager,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<AppendResponse> Append(AppendRequest request)
    {
        _executionContextManager.Establish(request.Namespace, CorrelationId.New(), request.EventStoreName);
        var eventSequence = GetEventSequence(request.EventStoreName, request.EventSequenceId, request.Namespace);
        await eventSequence.Append(
            request.EventSourceId,
            request.EventType.ToKernel(),
            JsonSerializer.Deserialize<JsonNode>(request.Content, _jsonSerializerOptions)!.AsObject(),
            request.Causation.ToKernel(),
            request.Identity.ToKernel(),
            request.ValidFrom);

        return new AppendResponse();
    }

    Grains.EventSequences.IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        _grainFactory.GetGrain<Grains.EventSequences.IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
}
