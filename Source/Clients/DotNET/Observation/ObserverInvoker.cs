// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverInvoker"/>.
/// </summary>
public class ObserverInvoker : IObserverInvoker
{
    readonly Dictionary<EventType, MethodInfo> _methodsByEventTypeId;
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IObserverMiddlewares _middlewares;
    readonly Type _targetType;
    readonly ILogger<ObserverInvoker> _logger;

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => _methodsByEventTypeId.Keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverInvoker"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances of actual observer.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="middlewares"><see cref="IObserverMiddlewares"/> to call.</param>
    /// <param name="targetType">Type of observer.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ObserverInvoker(
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IObserverMiddlewares middlewares,
        Type targetType,
        ILogger<ObserverInvoker> logger)
    {
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _middlewares = middlewares;
        _targetType = targetType;
        _logger = logger;
        _methodsByEventTypeId = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                        .Where(_ => IsObservingMethod(_))
                                        .ToDictionary(_ => _eventTypes.GetEventTypeFor(_.GetParameters()[0].ParameterType), _ => _);
    }

    /// <inheritdoc/>
    public async Task Invoke(object content, EventContext eventContext)
    {
        try
        {
            var actualObserver = _serviceProvider.GetService(_targetType);
            var eventType = _eventTypes.GetEventTypeFor(content.GetType());

            if (_methodsByEventTypeId.ContainsKey(eventType))
            {
                Task returnValue;
                var method = _methodsByEventTypeId[eventType];
                var parameters = method.GetParameters();

                await _middlewares.BeforeInvoke(eventContext, content);

                if (parameters.Length == 2)
                {
                    returnValue = (Task)method.Invoke(actualObserver, new object[] { content, eventContext })!;
                }
                else
                {
                    returnValue = (Task)method.Invoke(actualObserver, new object[] { content })!;
                }

                if (returnValue is not null) await returnValue;
                await _middlewares.AfterInvoke(eventContext, content);
            }
        }
        catch (Exception ex)
        {
            _logger.ObserverFailed(_targetType.FullName ?? _targetType.Name, content.GetType().Name, ex);
            throw;
        }
    }

    bool IsObservingMethod(MethodInfo methodInfo)
    {
        var isObservingMethod = (methodInfo.ReturnType.IsAssignableTo(typeof(Task)) && !methodInfo.ReturnType.IsGenericType) ||
                                methodInfo.ReturnType == typeof(void);

        if (!isObservingMethod) return false;
        var parameters = methodInfo.GetParameters();
        if (parameters.Length >= 1)
        {
            isObservingMethod = _eventTypes.HasFor(parameters[0].ParameterType);
            if (parameters.Length == 2)
            {
                isObservingMethod &= parameters[1].ParameterType == typeof(EventContext);
            }
            else if (parameters.Length > 2)
            {
                isObservingMethod = false;
            }
            return isObservingMethod;
        }

        return false;
    }
}
