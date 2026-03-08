// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Monads;
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
public class ReactorInvoker(
    IEventTypes eventTypes,
    IReactorMiddlewares middlewares,
    Type targetType,
    ActivatedArtifact activatedReactor,
    ILogger<ReactorInvoker> logger) : IReactorInvoker
{
    static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> _methodsByEventTypeCache = [];
    readonly Dictionary<Type, MethodInfo> _methodsByEventType = GetMethodsByEventType(targetType, eventTypes.AllClrTypes);

    /// <summary>
    /// Gets all <see cref="EventType"/> for a specific reactor type.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for looking up event types.</param>
    /// <param name="reactorType">The reactor <see cref="Type"/> to get event types for.</param>
    /// <returns>Collection of discovered <see cref="EventType"/>.</returns>
    public static IImmutableList<EventType> GetEventTypesFor(IEventTypes eventTypes, Type reactorType) =>
        GetMethodsByEventType(reactorType, eventTypes.AllClrTypes)
            .Keys
            .Select(eventTypes.GetEventTypeFor)
            .ToImmutableList();

    /// <inheritdoc/>
    public async Task<Catch> Invoke(object content, EventContext eventContext)
    {
        var reactorId = targetType.GetReactorId();
        var eventTypeName = content.GetType().Name;
        try
        {
            var eventType = content.GetType();

            if (_methodsByEventType.TryGetValue(eventType, out var method))
            {
                Task returnValue;
                var parameters = method.GetParameters();

                await middlewares.BeforeInvoke(eventContext, content);

                if (parameters.Length == 2)
                {
                    returnValue = (Task)method.Invoke(activatedReactor.Instance, [content, eventContext])!;
                }
                else
                {
                    returnValue = (Task)method.Invoke(activatedReactor.Instance, [content])!;
                }

                await returnValue;
            }
            else
            {
                logger.ReactorNoHandlerFound(reactorId, eventTypeName);
            }

            return Catch.Success();
        }
        catch (Exception ex)
        {
            logger.ReactorFailed(reactorId, eventTypeName, ex);
            return ex;
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

    static Dictionary<Type, MethodInfo> BuildMethodsByEventType(Type targetType, IEnumerable<Type> eventTypes)
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

    static Dictionary<Type, MethodInfo> GetMethodsByEventType(Type targetType, IEnumerable<Type> eventTypes) =>
        _methodsByEventTypeCache.GetOrAdd(
            targetType,
            static (key, keyEventTypes) => BuildMethodsByEventType(key, keyEventTypes),
            eventTypes);
}
