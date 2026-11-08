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
    /// Gets whether an observer type declares any method taking an <see cref="EventBatch"/>.
    /// </summary>
    /// <param name="observerType">The observer <see cref="Type"/>.</param>
    /// <returns>True if it does, false if not.</returns>
    public static bool DeclaresAny(Type observerType) => AllMethodsOf(observerType).Any(TakesEventBatch);

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

    /// <summary>
    /// Throws when a batch method declares a parameter Chronicle cannot supply when it calls the method.
    /// </summary>
    /// <param name="observerType">The observer <see cref="Type"/>.</param>
    /// <param name="method">The batch method.</param>
    /// <param name="leading">How many leading parameters Chronicle passes, starting with the <see cref="EventBatch"/>.</param>
    /// <exception cref="BatchMethodParameterCannotBeSupplied">A parameter cannot be supplied.</exception>
    /// <remarks>
    /// Mirrors what the analyzer reports as CHR0075, so a method the analyzer refuses is also refused at registration instead
    /// of failing every page it is called for.
    /// </remarks>
    public static void ThrowIfParametersCannotBeSupplied(Type observerType, MethodInfo method, int leading)
    {
        var problem = ProblemWithParameters(method, leading);
        if (problem is not null)
        {
            throw new BatchMethodParameterCannotBeSupplied(observerType, method.Name, problem);
        }
    }

    static string? ProblemWithParameters(MethodInfo method, int leading)
    {
        if (method.IsGenericMethodDefinition || method.ContainsGenericParameters)
        {
            return "a batch method cannot be generic";
        }

        var parameters = method.GetParameters();
        var irregular = Array.Find(parameters, parameter => parameter.ParameterType.IsByRef || parameter.IsOptional || parameter.IsDefined(typeof(ParamArrayAttribute), false));
        if (irregular is not null)
        {
            return $"parameter '{irregular.Name}' must be an ordinary parameter, not ref, out, in, params or optional";
        }

        return parameters.Skip(leading).Select(ProblemWithResolved).FirstOrDefault(problem => problem is not null);
    }

    static string? ProblemWithResolved(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        if (type == typeof(BatchContext) || type == typeof(CancellationToken))
        {
            return null;
        }

        if (type == typeof(EventContext))
        {
            return $"parameter '{parameter.Name}' is an EventContext; each event in the batch carries its own context";
        }

        if (type == typeof(EventBatch))
        {
            return $"parameter '{parameter.Name}' is a second EventBatch; a batch method takes one, first";
        }

        return type.IsValueType || type == typeof(string)
            ? $"parameter '{parameter.Name}' of type '{type.FullName}' cannot be resolved"
            : null;
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
