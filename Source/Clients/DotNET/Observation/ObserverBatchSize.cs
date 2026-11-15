// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the page bounds and concurrency an observer resolves to, from its <see cref="BatchSizeAttribute"/> and the defaults.
/// </summary>
/// <param name="MaxEvents">The most events a page holds.</param>
/// <param name="MaxBytes">The most bytes a page holds, or zero to leave it to the kernel.</param>
/// <param name="MaxDelay">How long the kernel may wait to fill a live page.</param>
/// <param name="MaxConcurrency">How many event sources of a reducer's page are reduced at the same time.</param>
internal sealed record ObserverBatchSize(int MaxEvents, int MaxBytes, TimeSpan MaxDelay, int MaxConcurrency)
{
    /// <summary>
    /// The default page size of an observer with only per-event methods.
    /// </summary>
    public const int DefaultMaxEventsForPerEventMethods = 100;

    /// <summary>
    /// The default page size of an observer with a batch method.
    /// </summary>
    public const int DefaultMaxEventsForBatchMethods = 500;

    static readonly ConcurrentDictionary<(Type ObserverType, bool HasBatchMethods), ObserverBatchSize> _cache = new();

    /// <summary>
    /// Resolves the batch size of an observer type, finding out whether it has batch methods from its methods.
    /// </summary>
    /// <param name="observerType">The type declaring the observer's methods.</param>
    /// <returns>The <see cref="ObserverBatchSize"/>.</returns>
    /// <exception cref="InvalidBatchSize">The <see cref="BatchSizeAttribute"/> declares a negative bound.</exception>
    public static ObserverBatchSize For(Type observerType) => For(observerType, BatchMethods.DeclaresAny(observerType));

    /// <summary>
    /// Resolves the batch size of an observer type whose methods are already known.
    /// </summary>
    /// <param name="observerType">The type declaring the observer's methods.</param>
    /// <param name="hasBatchMethods">Whether the observer has a batch method.</param>
    /// <returns>The <see cref="ObserverBatchSize"/>.</returns>
    /// <exception cref="InvalidBatchSize">The <see cref="BatchSizeAttribute"/> declares a negative bound.</exception>
    public static ObserverBatchSize For(Type observerType, bool hasBatchMethods) => _cache.GetOrAdd((observerType, hasBatchMethods), Resolve);

    /// <summary>
    /// Converts to the <see cref="PageDelivery"/> declared at registration.
    /// </summary>
    /// <returns>The <see cref="PageDelivery"/>.</returns>
    public PageDelivery ToContract() => new()
    {
        Enabled = true,
        MaxEventsPerPage = (uint)MaxEvents,
        MaxBytesPerPage = (uint)MaxBytes,
        MaxDelayMilliseconds = (uint)MaxDelay.TotalMilliseconds
    };

    static ObserverBatchSize Resolve((Type ObserverType, bool HasBatchMethods) key)
    {
        var (observerType, hasBatchMethods) = key;
        var attribute = observerType.GetCustomAttribute<BatchSizeAttribute>(true) ?? new BatchSizeAttribute();
        ThrowIfNegative(observerType, nameof(BatchSizeAttribute.MaxEvents), attribute.MaxEvents);
        ThrowIfNegative(observerType, nameof(BatchSizeAttribute.MaxBytes), attribute.MaxBytes);
        ThrowIfNegative(observerType, nameof(BatchSizeAttribute.MaxDelayMilliseconds), attribute.MaxDelayMilliseconds);
        ThrowIfNegative(observerType, nameof(BatchSizeAttribute.MaxConcurrency), attribute.MaxConcurrency);

        var defaultMaxEvents = hasBatchMethods ? DefaultMaxEventsForBatchMethods : DefaultMaxEventsForPerEventMethods;
        return new(
            attribute.MaxEvents > 0 ? attribute.MaxEvents : defaultMaxEvents,
            attribute.MaxBytes,
            TimeSpan.FromMilliseconds(attribute.MaxDelayMilliseconds),
            attribute.MaxConcurrency > 0 ? attribute.MaxConcurrency : Environment.ProcessorCount);
    }

    static void ThrowIfNegative(Type observerType, string property, int value)
    {
        if (value < 0)
        {
            throw new InvalidBatchSize(observerType, property, value);
        }
    }
}
