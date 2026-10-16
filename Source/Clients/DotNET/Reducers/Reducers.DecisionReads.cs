// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers;

public partial class Reducers
{
    /// <summary>Replays the complete source history with generation and dispatch evidence.</summary>
    /// <typeparam name="T">The admitted model.</typeparam>
    /// <param name="key">The raw source ID.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>A guarded value, not yet enrolled with a unit.</returns>
    /// <exception cref="DecisionReadRefused">Admission, generation or completeness cannot be established.</exception>
    /// <exception cref="InvalidOperationException">The reducer failed or the retry state is unreachable.</exception>
    internal async Task<DecisionRead<T>> ReadDecision<T>(ReadModelKey key, CancellationToken cancellationToken)
        where T : class
    {
        var model = typeof(T);
        var (admission, types) = ReducerDecisionReadAdmission.Assess(_eventStore, model);
        if (!admission.IsAdmitted) throw new DecisionReadRefused(admission.Reason!.Value, model);
        ReducerDecisionReadAdmission.ValidateKey(key, model);
        var handler = GetHandlerForReadModelType(model);
        var call = new CallContext(new CallOptions(cancellationToken: cancellationToken));
        var services = _servicesAccessor.Services.Sequences;
        for (var attempt = 0; attempt != 3; ++attempt)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var boundaryTask = services.TailSequenceNumber(
                new()
                {
                    EventStore = _eventStore.Name, Namespace = _eventStore.Namespace, EventSequenceId = EventSequenceId.Log
                },
                call);
            var probeTask = services.TailSequenceNumber(
                new()
                {
                    EventStore = _eventStore.Name, Namespace = _eventStore.Namespace, EventSequenceId = EventSequenceId.Log,
                    EventSourceId = key.Value, EventTypeIds = string.Join(',', types.Select(type => type.Id.Value))
                },
                call);
            await Task.WhenAll(boundaryTask, probeTask);
            var boundary = (EventSequenceNumber)(await boundaryTask).EnsureSuccess().SequenceNumber;
            var probe = (EventSequenceNumber)(await probeTask).EnsureSuccess().SequenceNumber;
            var response = await services.ForEventSourceIdAndEventTypes(
                new ForEventSourceIdAndEventTypesRequest
                {
                    EventStore = _eventStore.Name, Namespace = _eventStore.Namespace, EventSequenceId = EventSequenceId.Log,
                    EventSourceId = key.Value, EventTypeIds = string.Join(',', types.Select(type => type.Id.Value))
                },
                call);
            var fetched = response.EnsureSuccess().ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            var inputs = fetched.OrderBy(input => input.Context.SequenceNumber).Select(input =>
            {
                var declared = types.SingleOrDefault(type => type.Id.Value == input.Context.EventType.Id);
                if (declared is null || input.Context.EventSourceId != key.Value)
                {
                    throw new DecisionReadRefused(DecisionReadRefusalReason.FoldIncomplete, model);
                }
                return input.ForDecisionRead(_eventStore, declared, model, _jsonSerializerOptions);
            }).ToArray();
            ReduceResult result;
            using (ReadProtectionReadRegion.Suspend())
            {
                result = inputs.Length == 0
                    ? new(null, EventSequenceNumber.Unavailable, [], string.Empty)
                    : await handler.Invoker.Invoke(_serviceProvider, inputs, null);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to reduce read model '{model.Name}'. {string.Join(Environment.NewLine, result.ErrorMessages)}");
            }
            if (result.DispatchedEvents != fetched.Length)
            {
                throw new DecisionReadRefused(DecisionReadRefusalReason.ReducerDispatchIncomplete, model);
            }
            var last = result.LastSuccessfullyObservedEvent;
            if (probe.IsActualValue && (!last.IsActualValue || last.Value < probe.Value))
            {
                if (attempt < 2) continue;
                throw new DecisionReadRefused(DecisionReadRefusalReason.FoldIncomplete, model);
            }
            if (last.IsActualValue && (!boundary.IsActualValue || last.Value > boundary.Value) && attempt < 2) continue;
            return new(key, result.ReadModelState as T, _eventStore.Name, _eventStore.Namespace, boundary.IsUnavailable ? EventSequenceNumber.BeforeFirst : boundary, types);
        }
        throw new InvalidOperationException("Unreachable reducer decision read retry state.");
    }
}
