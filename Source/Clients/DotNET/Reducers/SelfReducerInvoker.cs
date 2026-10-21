// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Invokes static self-reduction through the shared reducer-handler contract without model activation.
/// </summary>
internal sealed class SelfReducerInvoker : IReducerInvoker
{
    readonly FrozenDictionary<Type, MethodInfo> _methods;

    /// <summary>
    /// Initializes a static invoker from the owning registry.
    /// </summary>
    /// <param name="eventTypes">The owning registry.</param>
    /// <param name="readModelType">The self-reducing model.</param>
    /// <param name="containerName">The model container.</param>
    internal SelfReducerInvoker(IEventTypes eventTypes, Type readModelType, ReadModelContainerName containerName)
    {
        ReadModelType = readModelType;
        ContainerName = containerName;
        _methods = SelfReducerMethods.Get(eventTypes, readModelType);
        ReplayHooks = new(eventTypes, readModelType);
        EventTypes = _methods.Keys.Select(eventTypes.GetEventTypeFor).ToImmutableList();
    }

    /// <summary>
    /// Gets the validated optional notification hooks without model activation.
    /// </summary>
    public SelfReducerReplayHooks ReplayHooks { get; }

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
    public Type ReadModelType { get; }

    /// <inheritdoc/>
    public ReadModelContainerName ContainerName { get; }

    /// <inheritdoc/>
    public async Task<ReduceResult> Invoke(IServiceProvider serviceProvider, IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent)
    {
        var current = initialReadModelContent;
        var last = EventSequenceNumber.Unavailable;
        var dispatched = 0;
        foreach (var input in eventsAndContexts)
        {
            if (!_methods.TryGetValue(input.Event.GetType(), out var method))
            {
                continue;
            }
            try
            {
                var result = method.Invoke(null, method.GetParameters().Length == 3 ? [input.Event, current, input.Context] : [input.Event, current]);
                if (result is Task task)
                {
                    await task;
                    result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
                }
                current = result;
                last = input.Context.SequenceNumber;
                ++dispatched;
            }
            catch (Exception error)
            {
                var failure = error is TargetInvocationException { InnerException: not null } invocation ? invocation.InnerException! : error;
                return new(current, last, failure.GetAllMessages(), failure.StackTrace ?? string.Empty);
            }
        }

        return new(current, last, [], string.Empty) { DispatchedEvents = dispatched };
    }
}
