// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Classifies only known, partition-independent definition shapes. This does not select a replay path.
/// </summary>
public static class PartitionReplayabilityClassifier
{
    static readonly Dictionary<Type, HashSet<string>> _knownMembers = new()
    {
        [typeof(ProjectionDefinition)] =
        [
            nameof(ProjectionDefinition.Owner), nameof(ProjectionDefinition.EventSequenceId),
            nameof(ProjectionDefinition.Identifier), nameof(ProjectionDefinition.ReadModel),
            nameof(ProjectionDefinition.IsActive), nameof(ProjectionDefinition.IsRewindable),
            nameof(ProjectionDefinition.InitialModelState), nameof(ProjectionDefinition.From),
            nameof(ProjectionDefinition.Join), nameof(ProjectionDefinition.Children),
            nameof(ProjectionDefinition.FromDerivatives), nameof(ProjectionDefinition.FromEvery),
            nameof(ProjectionDefinition.RemovedWith), nameof(ProjectionDefinition.RemovedWithJoin),
            nameof(ProjectionDefinition.FromEventProperty), nameof(ProjectionDefinition.LastUpdated),
            nameof(ProjectionDefinition.Tags), nameof(ProjectionDefinition.AutoMap),
            nameof(ProjectionDefinition.Nested), nameof(ProjectionDefinition.SubscribesToAllEvents),
            nameof(ProjectionDefinition.NoAutoMapProperties), nameof(ProjectionDefinition.Scope),
            nameof(ProjectionDefinition.GenerationDelivery), nameof(ProjectionDefinition.Version),
            nameof(ProjectionDefinition.HasLegacyStorageFields), nameof(ProjectionDefinition.IsKernelOwned),
            nameof(ProjectionDefinition.IsEmpty)
        ],
        [typeof(FromDefinition)] = [nameof(FromDefinition.Properties), nameof(FromDefinition.Key), nameof(FromDefinition.ParentKey)],
        [typeof(FromEveryDefinition)] =
        [
            nameof(FromEveryDefinition.Properties), nameof(FromEveryDefinition.IncludeChildren),
            nameof(FromEveryDefinition.AutoMap), nameof(FromEveryDefinition.Key)
        ],
        [typeof(RemovedWithDefinition)] = [nameof(RemovedWithDefinition.Key), nameof(RemovedWithDefinition.ParentKey)],
        [typeof(FromDerivatives)] = [nameof(FromDerivatives.EventTypes), nameof(FromDerivatives.From)]
    };

    /// <summary>
    /// Classifies a reducer as closed, a known independent projection as closed, and everything else as open.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="definition">The projection definition, required for projections.</param>
    /// <returns>The partition replayability.</returns>
    /// <remarks>
    /// Engine IsEventSourceKeyed alone cannot establish definition-level safety: it does not reject global scope,
    /// nested objects or unknown definition members, and currently rejects explicit event-source identity keys.
    /// Identity keys are therefore checked directly, not through ProjectionFactory's resolver flags.
    /// </remarks>
    public static PartitionReplayability Classify(ObserverType observerType, ProjectionDefinition? definition = null) =>
        observerType == ObserverType.Reducer ||
        (observerType == ObserverType.Projection && definition is not null && IsClosed(definition))
            ? PartitionReplayability.Closed
            : PartitionReplayability.Open;

    static bool IsClosed(ProjectionDefinition definition) =>
        HasKnownShape(definition) &&
        definition.Scope == ProjectionScope.Namespaced &&
        !definition.SubscribesToAllEvents &&
        definition.Join is { Count: 0 } &&
        definition.RemovedWithJoin is { Count: 0 } &&
        definition.Children is { Count: 0 } &&
        (definition.Nested is null || definition.Nested.Count == 0) &&
        definition.FromEventProperty is null &&
        HasClosedEventDefinitions(definition);

    static bool HasClosedEventDefinitions(ProjectionDefinition definition) =>
        definition.From is not null && definition.RemovedWith is not null && definition.FromDerivatives is not null &&
        HasKnownShape(definition.FromEvery) && IsEventSourceKey(definition.FromEvery.Key) &&
        definition.From.Values.All(IsClosedFrom) &&
        definition.RemovedWith.Values.All(removed => HasKnownShape(removed) && removed.ParentKey is null && IsEventSourceKey(removed.Key)) &&
        definition.FromDerivatives.All(derivatives => HasKnownShape(derivatives) && derivatives.EventTypes is not null && IsClosedFrom(derivatives.From)) &&
        (definition.From.Count > 0 || definition.RemovedWith.Count > 0 || definition.FromDerivatives.Any(derivatives => derivatives.EventTypes.Any()));

    static bool IsClosedFrom(FromDefinition from) =>
        HasKnownShape(from) && from.Properties is not null && from.ParentKey is null && IsEventSourceKey(from.Key);

    static bool IsEventSourceKey(PropertyExpression? key) =>
        string.IsNullOrEmpty(key?.Value) || string.Equals(key.Value, WellKnownExpressions.EventSourceId, StringComparison.Ordinal);

    static bool HasKnownShape<T>(T? definition)
        where T : class =>
        definition is not null && definition.GetType() == typeof(T) &&
        _knownMembers[typeof(T)].IsSupersetOf(typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.Name)) &&
        typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public).Length == 0;
}
