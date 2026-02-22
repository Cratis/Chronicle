// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerInvoker"/>.
/// </summary>
public class ReducerInvoker : IReducerInvoker
{
    static readonly ConcurrentDictionary<(Type TargetType, Type ReadModelType), Dictionary<Type, MethodInfo>> _methodsByEventTypeCache = [];
    readonly Dictionary<Type, MethodInfo> _methodsByEventType = [];
    readonly IClientArtifactsActivator _artifactActivator;
    readonly Type _targetType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerInvoker"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating reducer instances.</param>
    /// <param name="targetType">Type of reducer.</param>
    /// <param name="readModelType">Type of read model for the reducer.</param>
    /// <param name="containerName">Container name of the read model for the reducer.</param>
    public ReducerInvoker(
        IEventTypes eventTypes,
        IClientArtifactsActivator artifactActivator,
        Type targetType,
        Type readModelType,
        ReadModelContainerName containerName)
    {
        _artifactActivator = artifactActivator;
        _targetType = targetType;
        ReadModelType = readModelType;
        ContainerName = containerName;
        _methodsByEventType = _methodsByEventTypeCache.GetOrAdd(
            (targetType, readModelType),
            static (key, eventTypes) => BuildMethodsByEventType(key.TargetType, key.ReadModelType, eventTypes),
            eventTypes.AllClrTypes);

        EventTypes = _methodsByEventType.Keys.Select(eventTypes.GetEventTypeFor).ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
    public Type ReadModelType { get; }

    /// <inheritdoc/>
    public ReadModelContainerName ContainerName { get; }

    /// <inheritdoc/>
    public async Task<ReduceResult> Invoke(IServiceProvider serviceProvider, IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent)
    {
        var activatedReducerResult = _artifactActivator.Activate(serviceProvider, _targetType);
        if (activatedReducerResult.TryGetException(out var exception))
        {
            return new ReduceResult(
                initialReadModelContent,
                EventSequenceNumber.Unavailable,
                exception.GetAllMessages(),
                exception.StackTrace ?? string.Empty);
        }

        await using var activatedReducer = activatedReducerResult.AsT0;
        EventAndContext? lastSuccessfulObservedEventAndContext = default;
        var currentModelState = initialReadModelContent;

        foreach (var eventAndContext in eventsAndContexts)
        {
            var eventType = eventAndContext.Event.GetType();
            object returnValue = null!;

            try
            {
                if (_methodsByEventType.TryGetValue(eventType, out var method))
                {
                    var parameters = method.GetParameters();

                    if (parameters.Length == 3)
                    {
                        returnValue = method.Invoke(activatedReducer.Instance, [eventAndContext.Event, currentModelState, eventAndContext.Context])!;
                    }
                    else
                    {
                        returnValue = method.Invoke(activatedReducer.Instance, [eventAndContext.Event, currentModelState])!;
                    }

                    if (returnValue == null)
                    {
                        currentModelState = null;
                    }
                    else if (returnValue.GetType() == ReadModelType)
                    {
                        currentModelState = returnValue;
                    }
                    else if (returnValue is Task task)
                    {
                        await task;

                        if (task.GetType() == typeof(Task) ||
                            (task.GetType().IsGenericType &&
                             task.GetType().GetGenericTypeDefinition() == typeof(Task<>) &&
                             task.GetType().GetGenericArguments()[0].Name == "VoidTaskResult"))
                        {
                            currentModelState = null;
                        }
                        else
                        {
                            currentModelState = task.GetType().GetProperty(nameof(Task<int>.Result))?.GetValue(task);
                        }
                    }

                    lastSuccessfulObservedEventAndContext = eventAndContext;
                }
            }
            catch (Exception ex)
            {
                return new ReduceResult(
                            currentModelState,
                            lastSuccessfulObservedEventAndContext?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                            ex.GetAllMessages(),
                            ex.StackTrace ?? string.Empty);
            }
        }

        return new ReduceResult(
                currentModelState,
                lastSuccessfulObservedEventAndContext?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                [],
                string.Empty);
    }

    static Dictionary<Type, MethodInfo> BuildMethodsByEventType(Type targetType, Type readModelType, IEnumerable<Type> eventTypes)
    {
        var methodsByEventType = new Dictionary<Type, MethodInfo>();

        foreach (var method in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!method.IsReducerMethod(readModelType, eventTypes))
            {
                continue;
            }

            var eventParameterType = method.GetParameters()[0].ParameterType;
            foreach (var eventType in eventParameterType.GetEventTypes(eventTypes))
            {
                methodsByEventType[eventType] = method;
            }
        }

        return methodsByEventType;
    }
}
