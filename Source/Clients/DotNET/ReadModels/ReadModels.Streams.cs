// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels;

public partial class ReadModels
{
    /// <inheritdoc/>
    public async Task<TReadModel> GetInstanceById<TReadModel>(EventSourceId eventSourceId, EventStreamId eventStreamId)
    {
        ReadProtectionGuard.Read(nameof(GetInstanceById));
        var model = typeof(TReadModel);
        var scope = EventStreamReadModel.ScopeFor(model, eventStore.EventSources, eventSourceId, eventStreamId);
        if (!projections.HasFor(model)) throw new UnknownReadModel(model);
        var response = await _chronicleServicesAccessor.Services.ReadModels.GetInstanceByKey(new GetInstanceByKeyRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModelIdentifier = model.GetReadModelIdentifier(),
            EventSequenceId = GetEventSequenceIdFor(model),
            ReadModelKey = eventSourceId.Value,
            EventSourceId = scope.EventSourceId.Value,
            EventSourceType = scope.EventSourceType!.Value,
            EventStreamType = scope.EventStreamType.Value,
            EventStreamId = scope.EventStreamId.Value
        });
        if (!response.StreamScoped) throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeNotSupported, model);
        return (TReadModel)DeserializeInstance(model, eventSourceId.Value, response);
    }
}
