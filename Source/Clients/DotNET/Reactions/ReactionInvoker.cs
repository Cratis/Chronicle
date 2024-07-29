// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Conventions;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Represents an implementation of <see cref="IReactionInvoker"/>.
/// </summary>
public class ReactionInvoker : IReactionInvoker
{
    readonly Dictionary<Type, MethodInfo> _methodsByEventType;
    readonly IServiceProvider _serviceProvider;
    readonly IReactionMiddlewares _middlewares;
    readonly Type _targetType;
    readonly ILogger<ReactionInvoker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionInvoker"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances of actual reaction.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="middlewares"><see cref="IReactionMiddlewares"/> to call.</param>
    /// <param name="targetType">Type of reaction.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ReactionInvoker(
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IReactionMiddlewares middlewares,
        Type targetType,
        ILogger<ReactionInvoker> logger)
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
            var actualReaction = _serviceProvider.GetService(_targetType);
            var eventType = content.GetType();

            if (_methodsByEventType.ContainsKey(eventType))
            {
                Task returnValue;
                var method = _methodsByEventType[eventType];
                var parameters = method.GetParameters();

                await _middlewares.BeforeInvoke(eventContext, content);

                if (parameters.Length == 2)
                {
                    returnValue = (Task)method.Invoke(actualReaction, [content, eventContext])!;
                }
                else
                {
                    returnValue = (Task)method.Invoke(actualReaction, [content])!;
                }

                if (returnValue is not null) await returnValue;
                await _middlewares.AfterInvoke(eventContext, content);
            }
        }
        catch (Exception ex)
        {
            _logger.ReactionFailed(_targetType.GetReactionId(), content.GetType().Name, ex);
            throw;
        }
    }
}
