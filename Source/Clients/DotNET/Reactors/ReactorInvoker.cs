// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorInvoker"/>.
/// </summary>
public class ReactorInvoker : IReactorInvoker
{
    readonly Dictionary<Type, MethodInfo> _methodsByEventType;
    readonly IReactorMiddlewares _middlewares;
    readonly Type _targetType;
    readonly ILogger<ReactorInvoker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorInvoker"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="middlewares"><see cref="IReactorMiddlewares"/> to call.</param>
    /// <param name="targetType">Type of Reactor.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ReactorInvoker(
        IEventTypes eventTypes,
        IReactorMiddlewares middlewares,
        Type targetType,
        ILogger<ReactorInvoker> logger)
    {
        _middlewares = middlewares;
        _targetType = targetType;
        _logger = logger;
        _methodsByEventType = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(_ => _.IsEventHandlerMethod(eventTypes.AllClrTypes))
                                        .SelectMany(_ => _.GetParameters()[0].ParameterType.GetEventTypes(eventTypes.AllClrTypes).Select(eventType => (eventType, method: _)))
                                        .ToDictionary(_ => _.eventType, _ => _.method);

        EventTypes = _methodsByEventType.Keys.Select(eventTypes.GetEventTypeFor).ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
    public object CreateInstance(IServiceProvider serviceProvider) =>
        ActivatorUtilities.CreateInstance(serviceProvider, _targetType);

    /// <inheritdoc/>
    public async Task Invoke(object reactorInstance, object content, EventContext eventContext)
    {
        try
        {
            var eventType = content.GetType();

            if (_methodsByEventType.TryGetValue(eventType, out var method))
            {
                Task returnValue;
                var parameters = method.GetParameters();

                await _middlewares.BeforeInvoke(eventContext, content);

                if (parameters.Length == 2)
                {
                    returnValue = (Task)method.Invoke(reactorInstance, [content, eventContext])!;
                }
                else
                {
                    returnValue = (Task)method.Invoke(reactorInstance, [content])!;
                }

                await returnValue;
                await _middlewares.AfterInvoke(eventContext, content);
            }
        }
        catch (Exception ex)
        {
            _logger.ReactorFailed(_targetType.GetReactorId(), content.GetType().Name, ex);
            throw;
        }
    }
}
