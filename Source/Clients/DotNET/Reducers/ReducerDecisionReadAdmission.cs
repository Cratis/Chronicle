// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>Fail-closed admission for full, source-wide reducer replay.</summary>
internal static class ReducerDecisionReadAdmission
{
    /// <summary>Assesses the currently registered handler, without activating user code.</summary>
    /// <param name="store">The owning store.</param>
    /// <param name="model">The model type.</param>
    /// <param name="streamScoped">Whether the caller requested a narrower stream.</param>
    /// <returns>The admission and exact handler event identities.</returns>
    internal static (DecisionReadAdmission Admission, EventType[] Types) Assess(IEventStore store, Type model, bool streamScoped = false)
    {
        static (DecisionReadAdmission, EventType[]) Refuse(DecisionReadRefusalReason reason) => (new(false, reason), []);
        var handler = store.Reducers.GetHandlerForReadModelType(model);
        if (handler?.ReducerType is null) return Refuse(DecisionReadRefusalReason.Reducer);
        if (handler.EventSequenceId != EventSequenceId.Log) return Refuse(DecisionReadRefusalReason.NotEventLog);
        if (streamScoped) return Refuse(DecisionReadRefusalReason.StreamScopeNotSupported);
        var filter = ObserverEventFilter.For(handler.ReducerType, store.EventSources);
        if (filter.FilterTags.Count != 0 || filter.EventSourceType != EventSourceType.Unspecified || !filter.EventStreamType.IsAll)
        {
            return Refuse(DecisionReadRefusalReason.ReducerFilters);
        }
        if (!ReducerCatalog.IsSelfReducer(handler.ReducerType))
        {
            var constructors = handler.ReducerType.GetConstructors();
            if (constructors.Length == 0 || constructors.Any(constructor => constructor.GetParameters().Any(parameter =>
                !parameter.ParameterType.IsGenericType || parameter.ParameterType.GetGenericTypeDefinition() != typeof(ILogger<>))))
            {
                return Refuse(DecisionReadRefusalReason.ReducerDependencies);
            }
        }
        var types = handler.EventTypes.ToArray();
        if (types.Length == 0) return Refuse(DecisionReadRefusalReason.NoEventTypes);
        if (types.Any(type => type.Id.Value.Contains(','))) return Refuse(DecisionReadRefusalReason.UnsupportedEventTypeId);
        if (types.GroupBy(type => type.Id).Any(group => group.Select(type => type.Generation).Distinct().Count() != 1))
        {
            return Refuse(DecisionReadRefusalReason.ReducerGeneration);
        }
        return (new(true, null), types.DistinctBy(type => type.Id).ToArray());
    }

    /// <summary>Validates a raw event source key, without converting it through a model property.</summary>
    /// <param name="key">The raw source ID.</param>
    /// <param name="model">The refused model type.</param>
    /// <exception cref="DecisionReadRefused">The key is not a single raw source ID.</exception>
    internal static void ValidateKey(ReadModelKey key, Type model)
    {
        if (key is null || string.IsNullOrWhiteSpace(key.Value) || key.Value == "*" || key.Value.Contains('#') || key.Value != key.Value.Trim())
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.InvalidKey, model);
        }
    }
}
