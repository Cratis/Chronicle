// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Finds and validates the batch method of a reducer, and calls it for the events of one partition.
/// </summary>
internal static class ReducerBatchMethods
{
    /// <summary>
    /// Finds the batch method of a reducer, if it has one.
    /// </summary>
    /// <param name="reducerType">The type declaring the reducer methods.</param>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="eventTypes">The known event types.</param>
    /// <param name="hasPerEventMethods">Whether the reducer also has per-event methods.</param>
    /// <returns>The <see cref="BatchMethod"/>, or null when the reducer has none.</returns>
    /// <exception cref="ReducerHasMoreThanOneBatchMethod">The reducer declares more than one batch method.</exception>
    /// <exception cref="ReducerMixesBatchAndPerEventMethods">The reducer declares a batch method and per-event methods.</exception>
    /// <exception cref="InvalidReducerBatchMethod">The batch method does not take the current read model or return it.</exception>
    /// <exception cref="BatchMethodParameterCannotBeSupplied">The batch method declares a parameter Chronicle cannot supply.</exception>
    public static BatchMethod? Find(Type reducerType, Type readModelType, IEnumerable<Type> eventTypes, bool hasPerEventMethods)
    {
        var batchMethods = BatchMethods.For(reducerType, eventTypes);
        if (batchMethods.Count == 0)
        {
            return null;
        }

        if (batchMethods.Count > 1)
        {
            throw new ReducerHasMoreThanOneBatchMethod(reducerType);
        }

        if (hasPerEventMethods)
        {
            throw new ReducerMixesBatchAndPerEventMethods(reducerType);
        }

        var method = batchMethods[0].Method;
        var parameters = method.GetParameters();
        if (parameters.Length < 2 || parameters[1].ParameterType != readModelType)
        {
            throw new InvalidReducerBatchMethod(reducerType, method.Name, $"Take the current read model '{readModelType.Name}?' as the second parameter.");
        }

        if (method.ReturnType != readModelType && method.ReturnType != typeof(Task<>).MakeGenericType(readModelType))
        {
            throw new InvalidReducerBatchMethod(reducerType, method.Name, $"Return '{readModelType.Name}?' or 'Task<{readModelType.Name}?>'; null removes the read model.");
        }

        BatchMethods.ThrowIfParametersCannotBeSupplied(reducerType, method, 2);
        return batchMethods[0];
    }

    /// <summary>
    /// Calls the batch method of a reducer for the events of one partition.
    /// </summary>
    /// <param name="method">The <see cref="BatchMethod"/>.</param>
    /// <param name="instance">The reducer instance, or null for a static method.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for other parameters.</param>
    /// <param name="eventsAndContexts">The events of the partition, in order.</param>
    /// <param name="initialReadModelContent">The current read model state.</param>
    /// <param name="context">The <see cref="BatchContext"/> of the page.</param>
    /// <returns>The <see cref="ReduceResult"/>.</returns>
    public static async Task<ReduceResult> Invoke(
        BatchMethod method,
        object? instance,
        IServiceProvider serviceProvider,
        IEnumerable<EventAndContext> eventsAndContexts,
        object? initialReadModelContent,
        BatchContext context)
    {
        var events = eventsAndContexts.Where(_ => method.Observes(_.Event.GetType())).ToArray();
        if (events.Length == 0)
        {
            return new(initialReadModelContent, EventSequenceNumber.Unavailable, [], string.Empty);
        }

        var batch = new EventBatch(events, context.CanDefer);
        try
        {
            var result = method.Method.Invoke(instance, BindingFlags.DoNotWrapExceptions, null, method.ArgumentsFor([batch, initialReadModelContent], context, serviceProvider), null);
            if (result is Task task)
            {
                await task;
                result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
            }

            if (batch.Outcomes.Count > 0)
            {
                return FromOutcome(initialReadModelContent, batch.Outcomes[0]);
            }

            return new(result, events[^1].Context.SequenceNumber, [], string.Empty) { DispatchedEvents = events.Length };
        }
        catch (Exception exception)
        {
            return new(initialReadModelContent, EventSequenceNumber.Unavailable, exception.GetAllMessages(), exception.StackTrace ?? string.Empty)
            {
                ExceptionType = exception.GetType().FullName
            };
        }
    }

    static ReduceResult FromOutcome(object? initialReadModelContent, BatchOutcome outcome)
    {
        if (outcome.Kind == BatchOutcomeKind.Deferred)
        {
            return new(initialReadModelContent, EventSequenceNumber.Unavailable, [], string.Empty) { RetryAfter = outcome.RetryAfter };
        }

        var exception = outcome.Exception ?? new PartitionFailedInBatch(outcome.Partition);
        return new(initialReadModelContent, EventSequenceNumber.Unavailable, exception.GetAllMessages(), exception.StackTrace ?? string.Empty);
    }
}
