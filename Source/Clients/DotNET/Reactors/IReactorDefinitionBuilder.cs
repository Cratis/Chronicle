// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Builds the subscription for a runtime-registered reactor.
/// </summary>
public interface IReactorDefinitionBuilder
{
    /// <summary>
    /// Adds an event type, including its generation, to the subscription.
    /// </summary>
    /// <param name="eventType">The event type to observe.</param>
    /// <returns>The builder for continuation.</returns>
    IReactorDefinitionBuilder WithEventType(EventType eventType);

    /// <summary>
    /// Selects the event sequence to observe. Defaults to the event log.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <returns>The builder for continuation.</returns>
    IReactorDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Disables replay for this reactor.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IReactorDefinitionBuilder NotReplayable();
}
