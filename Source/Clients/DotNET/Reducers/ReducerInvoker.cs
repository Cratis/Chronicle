// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    readonly Dictionary<Type, MethodInfo> _methodsByEventType = [];
    readonly IArtifactActivator _artifactActivator;
    readonly Type _targetType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerInvoker"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="artifactActivator"><see cref="IArtifactActivator"/> for creating reducer instances.</param>
    /// <param name="targetType">Type of reducer.</param>
    /// <param name="readModelType">Type of read model for the reducer.</param>
    /// <param name="containerName">Container name of the read model for the reducer.</param>
    public ReducerInvoker(
        IEventTypes eventTypes,
        IArtifactActivator artifactActivator,
        Type targetType,
        Type readModelType,
        ReadModelContainerName containerName)
    {
        _artifactActivator = artifactActivator;
        _targetType = targetType;
        ReadModelType = readModelType;
        ContainerName = containerName;
        _methodsByEventType = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(_ => _.IsReducerMethod(readModelType, eventTypes.AllClrTypes))
                                        .SelectMany(_ => _.GetParameters()[0].ParameterType.GetEventTypes(eventTypes.AllClrTypes).Select(eventType => (eventType, method: _)))
                                        .ToDictionary(_ => _.eventType, _ => _.method);

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
        await using var activatedReducer = _artifactActivator.CreateInstance(serviceProvider, _targetType);
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
}
