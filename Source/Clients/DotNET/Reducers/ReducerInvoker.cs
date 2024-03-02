// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
    readonly Dictionary<Type, MethodInfo> _methodsByEventType = new();
    readonly IServiceProvider _serviceProvider;
    readonly Type _targetType;

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
        _targetType = targetType;
        ReadModelType = readModelType;
        _methodsByEventType = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
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
    public Task<InternalReduceResult> Invoke(IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent)
    {
        var actualReducer = _serviceProvider.GetRequiredService(_targetType);

        EventAndContext? lastSuccessfulObservedEventAndContext = default;

        foreach (var eventAndContext in eventsAndContexts)
        {
            var eventType = eventAndContext.Event.GetType();
            object returnValue = null!;

            try
            {
                if (_methodsByEventType.ContainsKey(eventType))
                {
                    var method = _methodsByEventType[eventType];
                    var parameters = method.GetParameters();

                    if (parameters.Length == 3)
                    {
                        returnValue = method.Invoke(actualReducer, new object[] { eventAndContext.Event, initialReadModelContent!, eventAndContext.Context })!;
                    }
                    else
                    {
                        returnValue = method.Invoke(actualReducer, new object[] { eventAndContext.Event, initialReadModelContent! })!;
                    }

                    if (returnValue.GetType() == ReadModelType)
                    {
                        initialReadModelContent = returnValue;
                    }
                    else
                    {
                        initialReadModelContent = _getResultMethod.GetGenericMethodDefinition().MakeGenericMethod(ReadModelType).Invoke(this, new[] { returnValue });
                    }

                    lastSuccessfulObservedEventAndContext = eventAndContext;
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                        new InternalReduceResult(
                            initialReadModelContent,
                            lastSuccessfulObservedEventAndContext?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                            ex.GetAllMessages(),
                            ex.StackTrace ?? string.Empty));
            }
        }

        return Task.FromResult(
            new InternalReduceResult(
                initialReadModelContent,
                lastSuccessfulObservedEventAndContext?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable,
                Enumerable.Empty<string>(),
                string.Empty));
    }

    TReadModel GetResult<TReadModel>(Task<TReadModel> task) => task.GetAwaiter().GetResult();
}
