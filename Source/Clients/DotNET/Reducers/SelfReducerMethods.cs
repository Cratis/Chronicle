// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Validates static state transitions and selects the uniquely most-specific method per registered event.
/// </summary>
internal static class SelfReducerMethods
{
    static readonly EventTypeRegistryCache<FrozenDictionary<Type, MethodInfo>> _cache = new();
    static readonly EventTypeSetCache<FrozenDictionary<Type, MethodInfo>> _sharedCache = new();

    /// <summary>
    /// Recognizes the structural signal without classifying classic reducers as self models.
    /// </summary>
    /// <param name="model">The candidate model.</param>
    /// <returns>Whether a valid static reduction method is declared.</returns>
    internal static bool HasSelfShape(Type model) => CanBeSelf(model) && ReduceMethods(model).Any(method => InvalidReason(model, method) is null);

    /// <summary>
    /// Includes near-misses for nonfatal discovery diagnostics.
    /// </summary>
    /// <param name="model">The candidate model.</param>
    /// <returns>Whether its methods warrant discovery inspection.</returns>
    internal static bool IsDiscoveryCandidate(Type model) => CanBeSelf(model) && ReduceMethods(model).Any(method =>
        method.IsStatic && (ReturnsModel(model, method) || method.GetParameters().ElementAtOrDefault(1)?.ParameterType == model));

    /// <summary>
    /// Refuses an explicitly nominated near-miss rather than silently ignoring it.
    /// </summary>
    /// <param name="model">The nominated type.</param>
    /// <exception cref="InvalidSelfReducerSignature">Only near-miss signatures exist.</exception>
    internal static void ValidateExplicit(Type model)
    {
        if (IsDiscoveryCandidate(model) && !HasSelfShape(model))
        {
            var method = ReduceMethods(model).First(method => method.IsStatic && (ReturnsModel(model, method) || method.GetParameters().ElementAtOrDefault(1)?.ParameterType == model));
            throw new InvalidSelfReducerSignature(model, method, InvalidReason(model, method)!);
        }
    }

    /// <summary>
    /// Gets the immutable dispatch map for the registry's CLR type set.
    /// </summary>
    /// <param name="eventTypes">The owning registry.</param>
    /// <param name="model">The self model.</param>
    /// <returns>The unique selected methods.</returns>
    internal static FrozenDictionary<Type, MethodInfo> Get(IEventTypes eventTypes, Type model) =>
        _cache.Get(eventTypes, model, model, types => _sharedCache.Get(types, model, model, snapshot => Build(model, snapshot)));

    /// <summary>
    /// Enumerates the declared hierarchy, retaining hidden methods for validation.
    /// </summary>
    /// <param name="model">The candidate model.</param>
    /// <returns>Methods declared throughout the hierarchy.</returns>
    internal static IEnumerable<MethodInfo> DeclaredMethods(Type model)
    {
        for (var type = model; type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                yield return method;
            }
        }
    }

    static bool CanBeSelf(Type model) => model.IsClass && !model.IsAbstract && !model.IsGenericType && !model.IsAssignableTo(typeof(IReducer));

    static IEnumerable<MethodInfo> ReduceMethods(Type model) => DeclaredMethods(model).Where(method => method.Name == "Reduce" && !method.IsSpecialName);

    static bool ReturnsModel(Type model, MethodInfo method) => method.ReturnType == model || method.ReturnType == typeof(Task<>).MakeGenericType(model);

    static FrozenDictionary<Type, MethodInfo> Build(Type model, IEnumerable<Type> eventTypes)
    {
        var methods = ReduceMethods(model).ToArray();
        if (methods.Length == 0)
        {
            throw new SelfReducerHasNoReduceMethods(model);
        }
        foreach (var method in methods)
        {
            if (InvalidReason(model, method) is { } reason)
            {
                throw new InvalidSelfReducerSignature(model, method, reason);
            }
        }

        var handledTypes = methods.ToDictionary(method => method, method => method.GetParameters()[0].ParameterType.GetEventTypes(eventTypes).ToFrozenSet());
        var selected = new Dictionary<Type, MethodInfo>();
        foreach (var eventType in eventTypes.Where(type => type != typeof(object)))
        {
            var candidates = methods.Where(method => handledTypes[method].Contains(eventType)).ToArray();
            if (candidates.Length > 0)
            {
                selected.Add(eventType, Select(model, eventType, candidates));
            }
        }

        return selected.ToFrozenDictionary();
    }

    static string? InvalidReason(Type model, MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (!method.IsPublic || !method.IsStatic || method.IsGenericMethod || method.ContainsGenericParameters ||
            parameters.Length is not (2 or 3) || parameters.Any(parameter => parameter.ParameterType.IsByRef || parameter.IsOptional || parameter.IsDefined(typeof(ParamArrayAttribute))))
        {
            return "Use public static non-generic Reduce(event, current[, EventContext]) without by-ref, optional or params arguments.";
        }
        if (parameters[0].ParameterType == typeof(object) || (!parameters[0].ParameterType.IsClass && !parameters[0].ParameterType.IsInterface) ||
            parameters[1].ParameterType != model || new NullabilityInfoContext().Create(parameters[1]).ReadState == NullabilityState.NotNull ||
            (parameters.Length == 3 && parameters[2].ParameterType != typeof(EventContext)))
        {
            return "Accept an event type, nullable current model and optionally EventContext. Nullable-oblivious current is also accepted.";
        }
        return ReturnsModel(model, method) ? null : "Return the model or Task of the model; null represents absent or deleted state.";
    }

    static MethodInfo Select(Type model, Type eventType, MethodInfo[] candidates)
    {
        var classes = candidates.Where(method => method.GetParameters()[0].ParameterType.IsClass).ToArray();
        var best = classes.Length > 0 ? MostSpecific(classes) : MostSpecific(candidates);
        if (best.Length != 1)
        {
            throw new AmbiguousSelfReducerMethod(model, eventType, best);
        }

        return best[0];
    }

    static MethodInfo[] MostSpecific(MethodInfo[] methods) => methods.Where(method => !methods.Any(other =>
        other.GetParameters()[0].ParameterType != method.GetParameters()[0].ParameterType &&
        other.GetParameters()[0].ParameterType.IsAssignableTo(method.GetParameters()[0].ParameterType))).ToArray();
}
