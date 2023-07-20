// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerInvoker"/>.
/// </summary>
public class ReducerInvoker : IReducerInvoker
{
    static readonly MethodInfo _getResultMethod = typeof(ReducerInvoker).GetMethod(nameof(GetResult), BindingFlags.Instance | BindingFlags.NonPublic)!;
    readonly Dictionary<EventType, MethodInfo> _reduceMethodsByEventType = new();
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly Type _targetType;
    readonly Type _readModelType;

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => _reduceMethodsByEventType.Keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerInvoker"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances of actual observer.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="targetType">Type of reducer.</param>
    /// <param name="readModelType">Type of read model for the reducer.</param>
    public ReducerInvoker(
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        Type targetType,
        Type readModelType)
    {
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _targetType = targetType;
        _readModelType = readModelType;
        _reduceMethodsByEventType = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                        .Where(_ => _.IsReducerMethod(readModelType))
                                        .ToDictionary(_ => eventTypes.GetEventTypeFor(_.GetParameters()[0].ParameterType), _ => _);
    }

    /// <inheritdoc/>
    public Task<object> Invoke(object eventContent, object? initialReadModelContent, EventContext eventContext) =>
        InvokeBulk(new[] { new EventAndContext(eventContent, eventContext) }, initialReadModelContent);

    /// <inheritdoc/>
    public Task<object> InvokeBulk(IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent)
    {
        var actualReducer = _serviceProvider.GetRequiredService(_targetType);

        foreach (var eventAndContext in eventsAndContexts)
        {
            var eventType = _eventTypes.GetEventTypeFor(eventAndContext.Event.GetType());
            object returnValue = null!;

            if (_reduceMethodsByEventType.ContainsKey(eventType))
            {
                var method = _reduceMethodsByEventType[eventType];
                var parameters = method.GetParameters();

                if (parameters.Length == 3)
                {
                    returnValue = method.Invoke(actualReducer, new object[] { eventAndContext.Event, initialReadModelContent!, eventAndContext.Context })!;
                }
                else
                {
                    returnValue = method.Invoke(actualReducer, new object[] { eventAndContext.Event, initialReadModelContent! })!;
                }

                if (returnValue.GetType() == _readModelType)
                {
                    initialReadModelContent = returnValue;
                }
                else
                {
                    initialReadModelContent = _getResultMethod.GetGenericMethodDefinition().MakeGenericMethod(_readModelType).Invoke(this, new[] { returnValue });
                }
            }
        }

        return Task.FromResult(initialReadModelContent!);
    }

    TReadModel GetResult<TReadModel>(Task<TReadModel> task) => task.GetAwaiter().GetResult();
}
