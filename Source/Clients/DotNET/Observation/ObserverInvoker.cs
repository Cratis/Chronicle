// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Conventions;
using Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverInvoker"/>.
/// </summary>
public class ObserverInvoker : IObserverInvoker
{
    readonly Dictionary<Type, MethodInfo> _methodsByEventType;
    readonly IServiceProvider _serviceProvider;
    readonly IObserverMiddlewares _middlewares;
    readonly Type _targetType;
    readonly ILogger<ObserverInvoker> _logger;

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
        _middlewares = middlewares;
        _targetType = targetType;
        _logger = logger;
        _methodsByEventType = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                        .Where(_ => _.IsEventHandlerMethod(eventTypes.AllClrTypes))
                                        .SelectMany(_ => _.GetParameters()[0].ParameterType.GetEventTypes(eventTypes.AllClrTypes).Select(eventType => (eventType, method: _)))
                                        .ToDictionary(_ => _.eventType, _ => _.method);

        EventTypes = _methodsByEventType.Keys.Select(eventTypes.GetEventTypeFor).ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
    public async Task Invoke(object content, EventContext eventContext)
    {
        try
        {
            var actualObserver = _serviceProvider.GetService(_targetType);
            var eventType = content.GetType();

            if (_methodsByEventType.ContainsKey(eventType))
            {
                Task returnValue;
                var method = _methodsByEventType[eventType];
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
}
