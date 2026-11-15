// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Records the consumer metrics, tagged with the binding id only.
/// </summary>
public interface IFederationConsumerMetrics
{
    /// <summary>
    /// Record events pulled from a producer.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <param name="count">The number of events.</param>
    void EventsPulled(string bindingId, int count);

    /// <summary>
    /// Record events appended to the local inbox.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <param name="count">The number of events.</param>
    void EventsAppended(string bindingId, int count);

    /// <summary>
    /// Record an event that was not appended.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <param name="reason">One of <see cref="FederationSkipReasons"/>.</param>
    void EventSkipped(string bindingId, string reason);

    /// <summary>
    /// Record a failed pull or append.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <param name="kind">A short, secret free classification.</param>
    void Error(string bindingId, string kind);

    /// <summary>
    /// Record the current lag behind the producer.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <param name="lag">The number of events behind.</param>
    void Lag(string bindingId, ulong lag);
}
