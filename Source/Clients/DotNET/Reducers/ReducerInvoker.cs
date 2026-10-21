// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers.Validators;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerInvoker"/>.
/// </summary>
public class ReducerInvoker : IReducerInvoker
{
    static readonly EventTypeRegistryCache<FrozenDictionary<Type, MethodInfo>> _methodsByEventTypeCache = new();
    readonly FrozenDictionary<Type, MethodInfo> _methodsByEventType;
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
        _methodsByEventType = GetMethodsFor(eventTypes, targetType, readModelType);

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
        var dispatched = 0;

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
                    ++dispatched;
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
                string.Empty) { DispatchedEvents = dispatched };
    }

    /// <summary>
    /// Gets the registered event identities from the same dispatch map used by reducer invocation.
    /// </summary>
    /// <param name="eventTypes">The owning event registry.</param>
    /// <param name="targetType">The reducer CLR type.</param>
    /// <param name="readModelType">The registered reducer's model type.</param>
    /// <returns>The accepted event identities.</returns>
    internal static IImmutableList<EventType> GetEventTypesFor(IEventTypes eventTypes, Type targetType, Type readModelType) =>
        GetMethodsFor(eventTypes, targetType, readModelType).Keys.Select(eventTypes.GetEventTypeFor).ToImmutableList();

    static FrozenDictionary<Type, MethodInfo> GetMethodsFor(IEventTypes eventTypes, Type targetType, Type readModelType) =>
        _methodsByEventTypeCache.Get(eventTypes, targetType, readModelType, types => BuildMethodsByEventType(targetType, readModelType, types));

    static FrozenDictionary<Type, MethodInfo> BuildMethodsByEventType(Type targetType, Type readModelType, IEnumerable<Type> eventTypes)
    {
        // A method that matches the reducer shape but declares its current read model as non-nullable used to be
        // dropped from dispatch without a word, so its events were never applied and the reducer merely looked
        // registered. Reject it here instead - it can only ever be a mistake (#3947).
        ReducerMethodCurrentReadModelMustBeNullable.ThrowIfAnyMethodHasNonNullableCurrentReadModel(targetType, readModelType, eventTypes);

        var methodsByEventType = new Dictionary<Type, MethodInfo>();

        // Ordered so the reducer method that should win comes first, and claimed with TryAdd so it keeps the
        // event type. A private helper shaped like a reducer method is a candidate too, and without this it
        // could overwrite the real one purely on reflection order.
        foreach (var method in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ByPrecedence())
        {
            if (!method.IsReducerMethod(readModelType, eventTypes))
            {
                continue;
            }

            var eventParameterType = method.GetParameters()[0].ParameterType;
            foreach (var eventType in eventParameterType.GetEventTypes(eventTypes))
            {
                methodsByEventType.TryAdd(eventType, method);
            }
        }

        return methodsByEventType.ToFrozenDictionary();
    }
}
