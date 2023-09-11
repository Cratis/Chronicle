// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Contracts.EventSequences;
using Aksio.Cratis.Kernel.Grains.EventSequences;

namespace Aksio.Cratis.Kernel.Services.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
public class EventSequences : IEventSequences
{
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequences"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventSequences(
        IGrainFactory grainFactory,
        IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task<AppendResponse> Append(AppendRequest request)
    {
        _executionContextManager.Establish(request.TenantId, _executionContextManager.Current.CorrelationId, request.MicroserviceId);
        var eventSequence = GetEventSequence(request.MicroserviceId, request.EventSequenceId, request.TenantId);
        await eventSequence.Append(
            request.EventSourceId,
            request.EventType,
            JsonObject.Parse(request.Content)!.AsObject(),
            request.Causation.ToKernel(),
            request.Identity.ToKernel(),
            request.ValidFrom);

        return new AppendResponse();
    }

    IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        _grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
}
