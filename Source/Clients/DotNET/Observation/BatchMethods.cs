// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Discovers the batch methods of an observer and enforces the rules that keep them apart from per-event methods.
/// </summary>
internal static class BatchMethods
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Gets the batch methods of an observer type.
    /// </summary>
    /// <param name="observerType">The observer <see cref="Type"/>.</param>
    /// <param name="eventTypes">The known event types.</param>
    /// <returns>The <see cref="BatchMethod"/> instances.</returns>
    /// <exception cref="ObservesOnlyOnBatchMethods">A method that does not take an <see cref="EventBatch"/> declares <see cref="ObservesAttribute"/>.</exception>
    /// <exception cref="BatchMethodObservesNothing">A batch method declares no <see cref="ObservesAttribute"/>.</exception>
    /// <exception cref="BatchMethodObservesUnknownEventType">A batch method observes a type that is not a known event type.</exception>
    /// <exception cref="EventTypeObservedByMoreThanOneMethod">Two batch methods observe the same event type.</exception>
    public static IReadOnlyList<BatchMethod> For(Type observerType, IEnumerable<Type> eventTypes)
    {
        var knownEventTypes = eventTypes as IList<Type> ?? [.. eventTypes];
        var batchMethods = new List<BatchMethod>();
        foreach (var method in AllMethodsOf(observerType))
        {
            var observes = method.GetCustomAttributes<ObservesAttribute>(true).ToArray();
            var takesBatch = method.TakesEventBatch();
            if (!takesBatch)
            {
                if (observes.Length > 0)
                {
                    throw new ObservesOnlyOnBatchMethods(observerType, method.Name);
                }
                continue;
            }

            if (observes.Length == 0)
            {
                throw new BatchMethodObservesNothing(observerType, method.Name);
            }

            var observed = new HashSet<Type>();
            foreach (var attribute in observes)
            {
                var matching = attribute.EventType.GetEventTypes(knownEventTypes).ToArray();
                if (matching.Length == 0)
                {
                    throw new BatchMethodObservesUnknownEventType(observerType, method.Name, attribute.EventType);
                }
                observed.UnionWith(matching);
            }

            var alreadyObserved = observed.FirstOrDefault(type => batchMethods.Exists(_ => _.Observes(type)));
            if (alreadyObserved is not null)
            {
                throw new EventTypeObservedByMoreThanOneMethod(observerType, alreadyObserved);
            }

            batchMethods.Add(new(method, observed.ToFrozenSet()));
        }

        return batchMethods;
    }

    /// <summary>
    /// Gets whether a method takes an <see cref="EventBatch"/> as its first parameter.
    /// </summary>
    /// <param name="method">The <see cref="MethodInfo"/>.</param>
    /// <returns>True if it does, false if not.</returns>
    public static bool TakesEventBatch(this MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length > 0 && parameters[0].ParameterType == typeof(EventBatch);
    }

    /// <summary>
    /// Throws when a per-event method takes a <see cref="BatchContext"/>.
    /// </summary>
    /// <param name="observerType">The observer <see cref="Type"/>.</param>
    /// <param name="perEventMethods">The per-event methods.</param>
    /// <exception cref="BatchContextOnlyForBatchMethods">A per-event method takes a <see cref="BatchContext"/>.</exception>
    public static void ThrowIfAnyTakesBatchContext(Type observerType, IEnumerable<MethodInfo> perEventMethods)
    {
        var offending = perEventMethods.FirstOrDefault(method => method.GetParameters().Any(_ => _.ParameterType == typeof(BatchContext)));
        if (offending is not null)
        {
            throw new BatchContextOnlyForBatchMethods(observerType, offending.Name);
        }
    }

    static IEnumerable<MethodInfo> AllMethodsOf(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var method in current.GetMethods(Flags).Where(_ => !_.IsSpecialName))
            {
                yield return method;
            }
        }
    }
}
