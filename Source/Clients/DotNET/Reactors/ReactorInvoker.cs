// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorInvoker"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorInvoker"/> class.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
/// <param name="middlewares"><see cref="IReactorMiddlewares"/> to call.</param>
/// <param name="targetType">Type of Reactor.</param>
/// <param name="activatedReactor">The <see cref="ActivatedArtifact"/> activated reactor.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="sideEffectHandlers">
/// Optional <see cref="IReactorSideEffectHandlers"/> used to process events returned by handler methods.
/// When <see langword="null"/>, any return values are silently discarded.
/// </param>
/// <param name="eventStore">
/// Optional <see cref="IEventStore"/> supplied to side effect handlers when appending events.
/// When <see langword="null"/>, any return values are silently discarded even if handlers are registered.
/// </param>
/// <param name="reactorContextValuesBuilder">
/// Optional <see cref="IReactorContextValuesBuilder"/> used to resolve append-metadata for side-effect events.
/// When <see langword="null"/>, no values are resolved and append-metadata falls back to the triggering event.
/// </param>
public class ReactorInvoker(
    IEventTypes eventTypes,
    IReactorMiddlewares middlewares,
    Type targetType,
    ActivatedArtifact activatedReactor,
    ILogger<ReactorInvoker> logger,
    IReactorSideEffectHandlers? sideEffectHandlers = null,
    IEventStore? eventStore = null,
    IReactorContextValuesBuilder? reactorContextValuesBuilder = null) : IReactorInvoker
{
    static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> _methodsByEventTypeCache = [];
    readonly Dictionary<Type, MethodInfo> _methodsByEventType = MethodsByEventType.Get(targetType, eventTypes.AllClrTypes);

    /// <summary>
    /// Gets all <see cref="EventType"/> for a specific reactor type.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for looking up event types.</param>
    /// <param name="reactorType">The reactor <see cref="Type"/> to get event types for.</param>
    /// <returns>Collection of discovered <see cref="EventType"/>.</returns>
    public static IImmutableList<EventType> GetEventTypesFor(IEventTypes eventTypes, Type reactorType) =>
        MethodsByEventType.Get(reactorType, eventTypes.AllClrTypes)
            .Keys
            .Select(eventTypes.GetEventTypeFor)
            .ToImmutableList();

    /// <inheritdoc/>
    public async Task<ReactorInvocationResult> Invoke(object content, EventContext eventContext)
    {
        var reactorId = targetType.GetReactorId();
        var eventTypeName = content.GetType().Name;
        try
        {
            var eventType = content.GetType();

            if (_methodsByEventType.TryGetValue(eventType, out var method))
            {
                object? returnValue;
                var parameters = method.GetParameters();

                await middlewares.BeforeInvoke(eventContext, content);

                if (parameters.Length == 2)
                {
                    returnValue = method.Invoke(activatedReactor.Instance, [content, eventContext]);
                }
                else
                {
                    returnValue = method.Invoke(activatedReactor.Instance, [content]);
                }

                var sideEffectFailure = await HandleReturnValue(method, returnValue, eventContext);
                if (sideEffectFailure is not null)
                {
                    return ReactorInvocationResult.FromSideEffectFailure(sideEffectFailure);
                }
            }
            else
            {
                logger.ReactorNoHandlerFound(reactorId, eventTypeName);
            }

            return ReactorInvocationResult.Success();
        }
        catch (Exception ex)
        {
            logger.ReactorFailed(reactorId, eventTypeName, ex);
            return ReactorInvocationResult.FromException(ex);
        }
        finally
        {
            // We cannot fail the whole reactor if the middlewares fail, because the event has been successfully reacted to.
            // So we catch and log any exceptions from the middlewares.
            try
            {
                await middlewares.AfterInvoke(eventContext, content);
            }
            catch (Exception ex)
            {
                logger.ReactorMiddlewareAfterInvokeFailed(reactorId, eventTypeName, ex);
            }
        }
    }

    async Task<ReactorSideEffectFailure?> HandleReturnValue(MethodInfo method, object? returnValue, EventContext eventContext)
    {
        if (method.ReturnType == typeof(void))
        {
            return null;
        }

        if (returnValue is Task task)
        {
            await task;

            if (!method.ReturnType.IsGenericType)
            {
                return null;
            }

            if (sideEffectHandlers is null || eventStore is null)
            {
                return null;
            }

            var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
            var result = resultProperty?.GetValue(task);
            if (result is null)
            {
                return null;
            }

            var reactorContext = new ReactorContext(eventContext, activatedReactor.Instance, BuildValues(eventContext));
            if (sideEffectHandlers.CanHandle(reactorContext, result))
            {
                var handleResult = await sideEffectHandlers.Handle(reactorContext, eventStore, result);
                if (!handleResult.IsSuccess && handleResult.TryGetError(out var failure) && failure is not null)
                {
                    return failure;
                }
            }
            else
            {
                logger.ReactorReturnValueNotHandled(targetType.GetReactorId(), result.GetType().Name);
            }

            return null;
        }

        // Synchronous side-effect return value (e.g. TEvent, IEnumerable<T>)
        if (sideEffectHandlers is null || eventStore is null || returnValue is null)
        {
            return null;
        }

        var syncReactorContext = new ReactorContext(eventContext, activatedReactor.Instance, BuildValues(eventContext));
        if (sideEffectHandlers.CanHandle(syncReactorContext, returnValue))
        {
            var handleResult = await sideEffectHandlers.Handle(syncReactorContext, eventStore, returnValue);
            if (!handleResult.IsSuccess && handleResult.TryGetError(out var failure) && failure is not null)
            {
                return failure;
            }
        }
        else
        {
            logger.ReactorReturnValueNotHandled(targetType.GetReactorId(), returnValue.GetType().Name);
        }

        return null;
    }

    ReactorContextValues BuildValues(EventContext eventContext) =>
        reactorContextValuesBuilder?.Build(activatedReactor.Instance, eventContext) ?? ReactorContextValues.Empty;

    static class MethodsByEventType
    {
        public static Dictionary<Type, MethodInfo> Get(Type targetType, IEnumerable<Type> eventTypes) =>
            _methodsByEventTypeCache.GetOrAdd(
                targetType,
                static (key, keyEventTypes) => Build(key, keyEventTypes),
                eventTypes);

        static Dictionary<Type, MethodInfo> Build(Type targetType, IEnumerable<Type> eventTypes)
        {
            var methodsByEventType = new Dictionary<Type, MethodInfo>();

            foreach (var method in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!method.IsEventHandlerMethod(eventTypes))
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
}

