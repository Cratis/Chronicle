// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Validates optional static replay hooks and resolves services only in their notification scope.
/// </summary>
internal sealed class SelfReducerReplayHooks
{
    static readonly FrozenDictionary<string, ReplayState> _states = new Dictionary<string, ReplayState>
    {
        ["BeginReplay"] = ReplayState.BeginReplay,
        ["EndReplay"] = ReplayState.EndReplay,
        ["BeginReplayPartition"] = ReplayState.BeginReplayPartition,
        ["EndReplayPartition"] = ReplayState.EndReplayPartition
    }.ToFrozenDictionary(StringComparer.Ordinal);
    readonly Type _model;
    readonly FrozenDictionary<ReplayState, MethodInfo> _methods;

    /// <summary>
    /// Validates all declared hooks without activating the model or any service.
    /// </summary>
    /// <param name="eventTypes">The owning registry used to reject event injection.</param>
    /// <param name="model">The structurally recognized self model.</param>
    internal SelfReducerReplayHooks(IEventTypes eventTypes, Type model)
    {
        _model = model;
        var methods = new Dictionary<ReplayState, MethodInfo>();
        foreach (var method in SelfReducerMethods.DeclaredMethods(model).Where(method => _states.ContainsKey(method.Name)))
        {
            var state = _states[method.Name];
            Validate(eventTypes, model, method, state);
            if (!methods.TryAdd(state, method))
            {
                throw new InvalidSelfReducerLifecycleHook(model, method, "Declare at most one hook per notification, including inherited declarations.");
            }
        }
        _methods = methods.ToFrozenDictionary();
    }

    /// <summary>
    /// Gets the validated hooks used by this invoker's implementation fingerprint.
    /// </summary>
    internal IEnumerable<MethodInfo> Methods => _methods.Values;

    /// <summary>
    /// Invokes an existing hook and awaits it before disposing its asynchronous scope.
    /// </summary>
    /// <param name="serviceProvider">The owning client's root provider.</param>
    /// <param name="state">The delivered notification.</param>
    /// <param name="partition">The delivered partition.</param>
    /// <returns>The hook's completion.</returns>
    /// <exception cref="InvalidSelfReducerLifecycleHook">A required service is unregistered or the hook returns a null task.</exception>
    internal async Task Invoke(IServiceProvider serviceProvider, ReplayState state, Partition partition)
    {
        if (!_methods.TryGetValue(state, out var method))
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var availability = provider.GetService<IServiceProviderIsService>();
        var partitioned = IsPartitioned(state);
        var parameters = method.GetParameters();
        var arguments = new object[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            if (partitioned && index == 0)
            {
                arguments[index] = partition;
                continue;
            }
            var service = parameters[index].ParameterType;
            if (availability?.IsService(service) == false)
            {
                throw new InvalidSelfReducerLifecycleHook(_model, method, $"Replay service '{service.FullName}' is not registered.");
            }
            arguments[index] = provider.GetRequiredService(service);
        }
        try
        {
            var task = method.Invoke(null, arguments) as Task ?? throw new InvalidSelfReducerLifecycleHook(_model, method, "A hook must return a non-null Task.");
            await task;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    static bool IsPartitioned(ReplayState state) => state is ReplayState.BeginReplayPartition or ReplayState.EndReplayPartition;

    static void Validate(IEventTypes eventTypes, Type model, MethodInfo method, ReplayState state)
    {
        var parameters = method.GetParameters();
        if (!method.IsPublic || !method.IsStatic || method.IsGenericMethod || method.ContainsGenericParameters || method.ReturnType != typeof(Task) ||
            parameters.Any(parameter => parameter.ParameterType.IsByRef || parameter.IsOptional || parameter.IsDefined(typeof(ParamArrayAttribute))))
        {
            throw new InvalidSelfReducerLifecycleHook(model, method, "Use public static non-generic hooks returning exactly Task, without by-ref, optional or params arguments.");
        }
        var partitioned = IsPartitioned(state);
        if (partitioned && (parameters.Length == 0 || parameters[0].ParameterType != typeof(Partition)))
        {
            throw new InvalidSelfReducerLifecycleHook(model, method, "Partition hooks require Partition as their first argument.");
        }
        foreach (var parameter in parameters.Skip(partitioned ? 1 : 0))
        {
            var type = parameter.ParameterType;
            if ((!type.IsClass && !type.IsInterface) || type.ContainsGenericParameters || type == model || type == typeof(EventContext) || type == typeof(Partition) ||
                eventTypes.AllClrTypes.Any(eventType => eventType.IsAssignableTo(type)))
            {
                throw new InvalidSelfReducerLifecycleHook(model, method, "Trailing arguments must be registered services, not events, the model, EventContext or Partition.");
            }
        }
    }
}
